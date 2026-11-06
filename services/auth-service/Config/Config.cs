namespace PolesTaxi.AuthService.Config;

public sealed class AppConfig
{
    public string RedisConnection { get; init; } = "127.0.0.1:6379";
    public TimeSpan AccessTtl { get; init; } = TimeSpan.FromMinutes(20);
    public TimeSpan RefreshTtl { get; init; } = TimeSpan.FromDays(14);

    public static AppConfig FromEnvironment()
    {
        return new AppConfig
        {
            RedisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "127.0.0.1:6379",
            AccessTtl = TimeSpan.FromSeconds(ParseInt("ACCESS_TTL_SECONDS", 1200)),
            RefreshTtl = TimeSpan.FromSeconds(ParseInt("REFRESH_TTL_SECONDS", 1_209_600))
        };
    }

    private static int ParseInt(string key, int fallback)
    {
        return int.TryParse(Environment.GetEnvironmentVariable(key), out var value) ? value : fallback;
    }
}