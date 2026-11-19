namespace PolesTaxi.OrderService.Config;

public sealed class AppConfig
{
    public string HttpAddr { get; init; } = "http://0.0.0.0:8084";
    public string GrpcAddr { get; init; } = "http://0.0.0.0:9084";
    public string ElasticsearchUri { get; init; } = "http://127.0.0.1:9200";
    public string IndexName { get; init; } = "orders";
    public string RedisConnection { get; init; } = "127.0.0.1:16379";

    public static AppConfig FromEnvironment()
    {
        return new AppConfig
        {
            HttpAddr = Environment.GetEnvironmentVariable("HTTP_ADDR") ?? "http://0.0.0.0:8084",
            GrpcAddr = Environment.GetEnvironmentVariable("GRPC_ADDR") ?? "http://0.0.0.0:9084",
            ElasticsearchUri = Environment.GetEnvironmentVariable("ELASTICSEARCH_URI") ?? "http://127.0.0.1:9200",
            IndexName = Environment.GetEnvironmentVariable("ELASTICSEARCH_INDEX") ?? "orders",
            RedisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "127.0.0.1:16379"
        };
    }
}