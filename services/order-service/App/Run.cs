using PolesTaxi.OrderService.App.Db.Elastic;
using PolesTaxi.OrderService.App.Db.Redis;
using PolesTaxi.OrderService.Config;
using PolesTaxi.OrderService.Repository.Elastic;
using PolesTaxi.OrderService.Repository.Redis;

namespace PolesTaxi.OrderService.App;

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
            var elastic = await ElasticStore.New(config.ElasticsearchUri, config.IndexName, cts.Token);
            Console.WriteLine("elasticsearch ready");

            var redis = await RedisClient.New(config.RedisConnection, cts.Token);
            using (redis)
            {
                var cache = new OrderRedisCache(redis.Database, TimeSpan.FromMinutes(10));
                _ = new OrderElasticRepository(elastic.Client, elastic.IndexName, cache);
                Console.WriteLine("redis ready");

                try { await Task.Delay(Timeout.Infinite, cts.Token); }
                catch (OperationCanceledException) { }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"order store init failed: {ex.Message}");
            return 1;
        }

        Console.WriteLine("shutdown");
        return 0;
    }
}