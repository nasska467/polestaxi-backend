namespace PolesTaxi.OrderService.Config;

public sealed class AppConfig
{
    public string ElasticsearchUri { get; init; } = "http://127.0.0.1:9200";
    public string IndexName { get; init; } = "orders";
    public string RedisConnection { get; init; } = "127.0.0.1:6379";

    public static AppConfig FromEnvironment()
    {
        return new AppConfig
        {
            ElasticsearchUri = Environment.GetEnvironmentVariable("ELASTICSEARCH_URI") ?? "http://127.0.0.1:9200",
            IndexName = Environment.GetEnvironmentVariable("ELASTICSEARCH_INDEX") ?? "orders",
            RedisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "127.0.0.1:6379"
        };
    }
}