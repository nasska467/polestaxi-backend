namespace PolesTaxi.AnalyticService.Config;

public sealed class AppConfig
{
    public string HttpAddr { get; init; } = "http://0.0.0.0:8086";
    public string GrpcAddr { get; init; } = "http://0.0.0.0:9086";
    public string ClickHouseConnection { get; init; } = "Host=127.0.0.1;Port=8123;Database=default;Username=default;Password=";
    public string? KafkaBrokers { get; init; } = "127.0.0.1:9092";

    public static AppConfig FromEnvironment()
    {
        return new AppConfig
        {
            HttpAddr = Environment.GetEnvironmentVariable("HTTP_ADDR") ?? "http://0.0.0.0:8086",
            GrpcAddr = Environment.GetEnvironmentVariable("GRPC_ADDR") ?? "http://0.0.0.0:9086",
            ClickHouseConnection = Environment.GetEnvironmentVariable("CLICKHOUSE_CONNECTION") ?? "Host=127.0.0.1;Port=8123;Database=default;Username=default;Password=",
            KafkaBrokers = Environment.GetEnvironmentVariable("KAFKA_BROKERS") ?? "127.0.0.1:9092"
        };
    }
}