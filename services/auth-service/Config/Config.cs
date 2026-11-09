namespace PolesTaxi.AuthService.Config;

public sealed class AppConfig
{
    public string RedisConnection { get; init; } = "127.0.0.1:16379";
    public string HttpAddr { get; init; } = "http://0.0.0.0:8082";
    public string GrpcAddr { get; init; } = "http://0.0.0.0:9082";
    public string UserGrpcAddr { get; init; } = "http://127.0.0.1:9081";
    public string JwtSecret { get; init; } = "dev-polestaxi-jwt-secret-key-32bytes!";
    public int AccessTtlSeconds { get; init; } = 1200;
    public int RefreshTtlSeconds { get; init; } = 1209600;

    public static AppConfig FromEnvironment()
    {
        return new AppConfig
        {
            RedisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "127.0.0.1:16379",
            HttpAddr = Environment.GetEnvironmentVariable("HTTP_ADDR") ?? "http://0.0.0.0:8082",
            GrpcAddr = Environment.GetEnvironmentVariable("GRPC_ADDR") ?? "http://0.0.0.0:9082",
            UserGrpcAddr = Environment.GetEnvironmentVariable("USER_GRPC_ADDR") ?? "http://127.0.0.1:9081",
            JwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "dev-polestaxi-jwt-secret-key-32bytes!",
            AccessTtlSeconds = int.Parse(Environment.GetEnvironmentVariable("ACCESS_TTL_SECONDS") ?? "1200"),
            RefreshTtlSeconds = int.Parse(Environment.GetEnvironmentVariable("REFRESH_TTL_SECONDS") ?? "1209600")
        };
    }
}