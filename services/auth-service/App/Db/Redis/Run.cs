using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using PolesTaxi.AuthService.App.Db.Redis;
using PolesTaxi.AuthService.Config;
using PolesTaxi.AuthService.Gateway;
using PolesTaxi.AuthService.Gateway.Grpc;
using PolesTaxi.AuthService.Handler.Grpc;
using PolesTaxi.AuthService.Repository;
using PolesTaxi.AuthService.Repository.Redis;
using PolesTaxi.AuthService.Service;
using PolesTaxi.Contracts.User.V1;
using PolesTaxi.Shared.Hosting;

namespace PolesTaxi.AuthService.App;

public static class Run
{
    public static async Task<int> ExecuteAsync(string[] args)
    {
        _ = args;
        var config = AppConfig.FromEnvironment();
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        var builder = WebApplication.CreateBuilder();

        Observability.Add(builder, "auth-service");

        // Kestrel: 8082 (HTTP) и 9082 (gRPC)
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(8082);
            options.ListenAnyIP(9082, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
        });

        builder.Services.AddPolesTaxiGrpc();

        // Redis
        builder.Services.AddSingleton(_ => RedisClient.New(config.RedisConnection, cts.Token).GetAwaiter().GetResult());
        builder.Services.AddScoped<IAuthRepository>(sp => new AuthRedisRepository(sp.GetRequiredService<RedisClient>().Database));

        // gRPC-клиент к User с политикой Polly
        builder.Services.AddGrpcClient<UserService.UserServiceClient>(o => o.Address = new Uri(config.UserGrpcAddr))
            .AddPolesTaxiResilience();
        builder.Services.AddScoped<IUserGateway, UserGrpcGateway>();

        // Сервис аутентификации
        builder.Services.AddScoped<IAuthAppService>(sp => new AuthAppService(
            sp.GetRequiredService<IAuthRepository>(),
            sp.GetRequiredService<IUserGateway>(),
            config.JwtSecret,
            TimeSpan.FromSeconds(config.AccessTtlSeconds),
            TimeSpan.FromSeconds(config.RefreshTtlSeconds)));

        builder.Services.AddScoped<AuthGrpcHandler>();

        var app = builder.Build();

        app.UseRouting();
        app.MapGrpcService<AuthGrpcHandler>();
        app.UsePlatform(_ =>
        {
            var redis = app.Services.GetRequiredService<RedisClient>();
            return Task.FromResult(redis.Multiplexer.IsConnected);
        });

        await app.StartAsync(cts.Token);
        Console.WriteLine($"auth-service ready http={config.HttpAddr} grpc={config.GrpcAddr}");

        try { await Task.Delay(Timeout.Infinite, cts.Token); }
        catch (OperationCanceledException) { }

        await app.StopAsync();
        Console.WriteLine("shutdown");
        return 0;
    }
}
