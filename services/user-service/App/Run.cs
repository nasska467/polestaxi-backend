using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PolesTaxi.Shared.Hosting;
using PolesTaxi.UserService.App.Db.Mongo;
using PolesTaxi.UserService.Config;
using PolesTaxi.UserService.Entity.Repository;
using PolesTaxi.UserService.Handler.Grpc;
using PolesTaxi.UserService.Repository;
using PolesTaxi.UserService.Repository.Mongo;
using PolesTaxi.UserService.Service;
using MongoClient = PolesTaxi.UserService.App.Db.Mongo.MongoClient; // <-- добавили псевдоним

namespace PolesTaxi.UserService.App;

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
            options.ListenAnyIP(8081);
            options.ListenAnyIP(9081, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
        });

        builder.Services.AddPolesTaxiGrpc();

        builder.Services.AddSingleton(config);
        builder.Services.AddSingleton(_ => MongoClient.New(config.MongoConnection, config.MongoDatabase, cts.Token).GetAwaiter().GetResult());

        builder.Services.AddSingleton<IMongoCollection<UserDocument>>(sp =>
        {
            var client = sp.GetRequiredService<MongoClient>();
            return client.Users;
        });

        builder.Services.AddScoped<IUserRepository, UserMongoRepository>();
        builder.Services.AddScoped<IUserAppService, UserAppService>();
        builder.Services.AddScoped<UserGrpcHandler>();

        var app = builder.Build();

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = exception?.Message,
                    stack = exception?.StackTrace
                });
            });
        });

        app.UseRouting();
        app.MapGrpcService<UserGrpcHandler>();
        app.MapHealth();

        await app.StartAsync(cts.Token);
        Console.WriteLine($"user-service ready http={config.HttpAddr} grpc={config.GrpcAddr}");

        try { await Task.Delay(Timeout.Infinite, cts.Token); }
        catch (OperationCanceledException) { }

        await app.StopAsync();
        Console.WriteLine("shutdown");
        return 0;
    }
}