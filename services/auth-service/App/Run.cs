using PolesTaxi.AuthService.App.Db.Redis;
using PolesTaxi.AuthService.Config;
using PolesTaxi.AuthService.Repository.Redis;

namespace PolesTaxi.AuthService.App;

public static class Run
{
    public static async Task<int> ExecuteAsync(string[] args)
    {
        _ = args;
        var config = AppConfig.FromEnvironment();
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        RedisClient redis;
        try
        {
            redis = await RedisClient.New(config.RedisConnection, cts.Token);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"redis init failed: {ex.Message}");
            return 1;
        }

        using (redis)
        {
            var repo = new AuthRedisRepository(redis.Database);
            Console.WriteLine("redis ready");

            try { await Task.Delay(Timeout.Infinite, cts.Token); }
            catch (OperationCanceledException) { }
        }

        Console.WriteLine("shutdown");
        return 0;
    }
}