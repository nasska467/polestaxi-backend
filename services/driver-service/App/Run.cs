using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PolesTaxi.DriverService.App.Db.Mongo;
using PolesTaxi.DriverService.Config;
using PolesTaxi.DriverService.Entity.Repository;
using PolesTaxi.DriverService.Handler.Grpc;
using PolesTaxi.DriverService.Repository;
using PolesTaxi.DriverService.Repository.Mongo;
using PolesTaxi.DriverService.Service;
using PolesTaxi.Shared.Hosting;
using MongoClient = PolesTaxi.DriverService.App.Db.Mongo.MongoClient;

namespace PolesTaxi.DriverService.App;

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
            options.ListenAnyIP(8083);
            options.ListenAnyIP(9083, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
        });

        builder.Services.AddPolesTaxiGrpc();
        builder.Services.AddSingleton(config);
        builder.Services.AddSingleton(_ => MongoClient.New(config.MongoConnection, config.MongoDatabase, cts.Token).GetAwaiter().GetResult());
        builder.Services.AddSingleton<IMongoCollection<DriverDocument>>(sp => sp.GetRequiredService<MongoClient>().Drivers);
        builder.Services.AddScoped<IDriverRepository, DriverMongoRepository>();
        builder.Services.AddScoped<IDriverAppService, DriverAppService>();
        builder.Services.AddScoped<DriverGrpcHandler>();

        var app = builder.Build();

        app.UseRouting();
        app.MapGrpcService<DriverGrpcHandler>();
        app.MapHealth();

        await app.StartAsync(cts.Token);
        Console.WriteLine($"driver-service ready http={config.HttpAddr} grpc={config.GrpcAddr}");

        try { await Task.Delay(Timeout.Infinite, cts.Token); }
        catch (OperationCanceledException) { }

        await app.StopAsync();
        Console.WriteLine("shutdown");
        return 0;
    }
}