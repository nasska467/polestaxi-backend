namespace PolesTaxi.AnalyticService.Config;

public sealed class AppConfig
{
    public string ClickHouseConnection { get; init; } = "Host=127.0.0.1;Port=8123;Database=default";

    public static AppConfig FromEnvironment()
    {
        return new AppConfig
        {
            ClickHouseConnection = Environment.GetEnvironmentVariable("CLICKHOUSE_CONNECTION") ?? "Host=127.0.0.1;Port=8123;Database=default"
        };
    }
}