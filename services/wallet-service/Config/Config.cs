namespace PolesTaxi.WalletService.Config;

public sealed class AppConfig
{
    public string PostgresConnection { get; init; } = "Host=127.0.0.1;Port=5432;Database=polestaxi_wallet;Username=polestaxi;Password=polestaxi";
    public string RedisConnection { get; init; } = "127.0.0.1:6379";

    public static AppConfig FromEnvironment()
    {
        return new AppConfig
        {
            PostgresConnection = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION") ?? "Host=127.0.0.1;Port=5432;Database=polestaxi_wallet;Username=polestaxi;Password=polestaxi",
            RedisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "127.0.0.1:6379"
        };
    }
}