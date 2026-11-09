using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using PolesTaxi.OrderService.App.Db.Elastic;
using PolesTaxi.OrderService.App.Db.Redis;
using PolesTaxi.OrderService.Config;
using PolesTaxi.OrderService.Handler.Grpc;
using PolesTaxi.OrderService.Repository;
using PolesTaxi.OrderService.Repository.Elastic;
using PolesTaxi.OrderService.Repository.Redis;
using PolesTaxi.OrderService.Service;
using PolesTaxi.Shared.Hosting;

namespace PolesTaxi.OrderService.App;

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
            options.ListenAnyIP(8084);
            options.ListenAnyIP(9084, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
        });

        builder.Services.AddPolesTaxiGrpc();

        // Elasticsearch
        builder.Services.AddSingleton(_ => ElasticStore.New(config.ElasticsearchUri, config.IndexName, cts.Token).GetAwaiter().GetResult());

        // Redis
        builder.Services.AddSingleton(_ => RedisClient.New(config.RedisConnection, cts.Token).GetAwaiter().GetResult());
        builder.Services.AddSingleton(sp => new OrderRedisCache(sp.GetRequiredService<RedisClient>().Database, TimeSpan.FromMinutes(10)));

        // Репозиторий и сервис
        builder.Services.AddScoped<IOrderRepository>(sp => new OrderElasticRepository(
            sp.GetRequiredService<ElasticStore>().Client,
            sp.GetRequiredService<ElasticStore>().IndexName,
            sp.GetRequiredService<OrderRedisCache>()));

        builder.Services.AddScoped<IOrderAppService, OrderAppService>();
        builder.Services.AddScoped<OrderGrpcHandler>();

        var app = builder.Build();

        app.UseRouting();
        app.MapGrpcService<OrderGrpcHandler>();
        app.MapHealth();

        await app.StartAsync(cts.Token);
        Console.WriteLine($"order-service ready http={config.HttpAddr} grpc={config.GrpcAddr}");

        try { await Task.Delay(Timeout.Infinite, cts.Token); }
        catch (OperationCanceledException) { }

        await app.StopAsync();
        Console.WriteLine("shutdown");
        return 0;
    }
}