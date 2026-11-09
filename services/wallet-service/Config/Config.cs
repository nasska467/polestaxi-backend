namespace PolesTaxi.WalletService.Config;

public sealed class AppConfig
{
    public string HttpAddr { get; init; } = "http://0.0.0.0:8085";
    public string GrpcAddr { get; init; } = "http://0.0.0.0:9085";
    public string PostgresConnection { get; init; } = "Host=127.0.0.1;Port=15432;Database=polestaxi_wallet;Username=polestaxi;Password=polestaxi";
    public string RedisConnection { get; init; } = "127.0.0.1:16379";

    public static AppConfig FromEnvironment()
    {
        return new AppConfig
        {
            HttpAddr = Environment.GetEnvironmentVariable("HTTP_ADDR") ?? "http://0.0.0.0:8085",
            GrpcAddr = Environment.GetEnvironmentVariable("GRPC_ADDR") ?? "http://0.0.0.0:9085",
            PostgresConnection = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION") ?? "Host=127.0.0.1;Port=15432;Database=polestaxi_wallet;Username=polestaxi;Password=polestaxi",
            RedisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "127.0.0.1:16379"
        };
    }
}