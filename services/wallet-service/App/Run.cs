using PolesTaxi.WalletService.App.Db.Pg;
using PolesTaxi.WalletService.App.Db.Redis;
using PolesTaxi.WalletService.Config;
using PolesTaxi.WalletService.Repository.Pg;
using PolesTaxi.WalletService.Repository.Redis;

namespace PolesTaxi.WalletService.App;

public static class Run
{
    public static async Task<int> ExecuteAsync(string[] args)
    {
        _ = args;
        var config = AppConfig.FromEnvironment();
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        try
        {
            using var pg = PgClient.New(config.PostgresConnection);
            Console.WriteLine("postgres ready");

            var redis = await RedisClient.New(config.RedisConnection, cts.Token);
            using (redis)
            {
                var cache = new TransactionRedisCache(redis.Database, TimeSpan.FromMinutes(5));
                var repo = new WalletPgRepository(pg.DataSource, cache);
                Console.WriteLine("redis ready");

                try { await Task.Delay(Timeout.Infinite, cts.Token); }
                catch (OperationCanceledException) { }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"wallet store init failed: {ex.Message}");
            return 1;
        }

        Console.WriteLine("shutdown");
        return 0;
    }
}