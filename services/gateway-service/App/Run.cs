using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using PolesTaxi.Contracts.Auth.V1;
using PolesTaxi.GatewayService.App;
using PolesTaxi.GatewayService.Config;

namespace PolesTaxi.GatewayService.App;

public static class Run
{
    public static async Task<int> ExecuteAsync(string[] args)
    {
        _ = args;
        var config = AppConfig.FromEnvironment();
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        var builder = WebApplication.CreateBuilder();

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(8080);
        });

        // gRPC-клиент к Auth для проверки токенов
        builder.Services.AddGrpcClient<AuthService.AuthServiceClient>(o => o.Address = new Uri(config.AuthGrpcAddr));

        // YARP
        builder.Services.AddReverseProxy()
            .LoadFromMemory(YarpConfig.Routes(config), YarpConfig.Clusters(config));

        var app = builder.Build();

        app.UseMiddleware<GatewayMiddleware>();
        app.MapReverseProxy();
        app.MapGet("/healthz", () => Results.Text("ok"));

        await app.StartAsync(cts.Token);
        Console.WriteLine($"gateway ready http=http://0.0.0.0:8080");

        try { await Task.Delay(Timeout.Infinite, cts.Token); }
        catch (OperationCanceledException) { }

        await app.StopAsync();
        Console.WriteLine("shutdown");
        return 0;
    }
}