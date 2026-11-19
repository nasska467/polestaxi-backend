using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using PolesTaxi.AnalyticService.App.Db.ClickHouse;
using PolesTaxi.AnalyticService.Config;
using PolesTaxi.AnalyticService.Handler.Http;
using PolesTaxi.AnalyticService.Handler.Kafka;
using PolesTaxi.AnalyticService.Repository;
using PolesTaxi.AnalyticService.Repository.ClickHouse;
using PolesTaxi.Shared.Hosting;

namespace PolesTaxi.AnalyticService.App;

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
            options.ListenAnyIP(8086);
            options.ListenAnyIP(9086, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
        });

        builder.Services.AddSingleton<IAnalyticRepository>(_ => new AnalyticClickHouseRepository(config.ClickHouseConnection));
        builder.Services.AddScoped<AnalyticHandler>();

        var app = builder.Build();

        // HTTP-эндпоинты
        app.MapGet("/v1/analytics/orders", async (AnalyticHandler h, HttpContext ctx) => await h.GetOrders(ctx));
        app.MapGet("/v1/analytics/ratings", async (AnalyticHandler h, HttpContext ctx) => await h.GetRatings(ctx));
        app.MapGet("/v1/analytics/dashboard", async (AnalyticHandler h, HttpContext ctx) => await h.GetDashboard(ctx));
        app.MapHealth();

        await app.StartAsync(cts.Token);

        // Запускаем Kafka-консьюмер (если заданы брокеры)
        if (!string.IsNullOrWhiteSpace(config.KafkaBrokers))
        {
            var repo = app.Services.GetRequiredService<IAnalyticRepository>();
            var consumer = new KafkaHandler(repo, config.KafkaBrokers);
            _ = Task.Run(() => consumer.RunAsync(cts.Token), cts.Token);
        }

        Console.WriteLine($"analytic-service ready http={config.HttpAddr} grpc={config.GrpcAddr}");

        try { await Task.Delay(Timeout.Infinite, cts.Token); }
        catch (OperationCanceledException) { }

        await app.StopAsync();
        Console.WriteLine("shutdown");
        return 0;
    }
}