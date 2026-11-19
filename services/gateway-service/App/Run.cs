using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using PolesTaxi.Contracts.Auth.V1;
using PolesTaxi.GatewayService.App;
using PolesTaxi.GatewayService.Config;
using PolesTaxi.Shared.Hosting;
using Prometheus;

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

        Observability.Add(builder, "gateway-service");

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(8080);
        });

        // gRPC-клиент к Auth для проверки токенов с политикой Polly
        builder.Services.AddGrpcClient<AuthService.AuthServiceClient>(o => o.Address = new Uri(config.AuthGrpcAddr))
            .AddPolesTaxiResilience();

        // YARP
        builder.Services.AddReverseProxy()
            .LoadFromMemory(YarpConfig.Routes(config), YarpConfig.Clusters(config));

        var app = builder.Build();

        app.UseMiddleware<CorrelationMiddleware>();
        app.UseHttpMetrics();
        app.UseMiddleware<GatewayMiddleware>();
        app.MapReverseProxy();
        app.MapOps();

        await app.StartAsync(cts.Token);
        Console.WriteLine($"gateway ready http=http://0.0.0.0:8080");

        try { await Task.Delay(Timeout.Infinite, cts.Token); }
        catch (OperationCanceledException) { }

        await app.StopAsync();
        Console.WriteLine("shutdown");
        return 0;
    }
}
