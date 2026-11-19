using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using PolesTaxi.Shared.Hosting;
using PolesTaxi.WalletService.App.Db.Pg;
using PolesTaxi.WalletService.Config;
using PolesTaxi.WalletService.Handler.Grpc;
using PolesTaxi.WalletService.Repository;
using PolesTaxi.WalletService.Repository.Pg;
using PolesTaxi.WalletService.Service;

namespace PolesTaxi.WalletService.App;

public static class Run
{
    public static async Task<int> ExecuteAsync(string[] args)
    {
        _ = args;
        var config = AppConfig.FromEnvironment();
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        var builder = WebApplication.CreateBuilder();

        Observability.Add(builder, "wallet-service");

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(8085);
            options.ListenAnyIP(9085, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
        });

        builder.Services.AddPolesTaxiGrpc();
        builder.Services.AddSingleton(config);
        builder.Services.AddSingleton(_ => PgClient.New(config.PostgresConnection));
        
        builder.Services.AddScoped<IWalletRepository>(sp => new WalletPgRepository(sp.GetRequiredService<PgClient>().DataSource));
        
        builder.Services.AddScoped<IWalletAppService>(sp => 
            new WalletAppService(
                sp.GetRequiredService<IWalletRepository>(), 
                ParseBonus()
            ));
            
        builder.Services.AddScoped<WalletGrpcHandler>();

        var app = builder.Build();

        app.UseRouting();
        app.MapGrpcService<WalletGrpcHandler>();
        app.UsePlatform(async ct =>
        {
            var pg = app.Services.GetRequiredService<PgClient>();
            await using var conn = await pg.DataSource.OpenConnectionAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync(ct);
            return true;
        });

        await app.StartAsync(cts.Token);
        Console.WriteLine($"wallet-service ready http={config.HttpAddr} grpc={config.GrpcAddr}");

        try { await Task.Delay(Timeout.Infinite, cts.Token); }
        catch (OperationCanceledException) { }

        await app.StopAsync();
        Console.WriteLine("shutdown");
        return 0;
    }

    private static decimal ParseBonus()
    {
        var env = Environment.GetEnvironmentVariable("WALLET_INITIAL_BALANCE");
        return decimal.TryParse(env, out var bonus) ? bonus : 5000m;
    }
}
