namespace PolesTaxi.UserService.Config;

public sealed class AppConfig
{
    public string MongoConnection { get; init; } = "mongodb://127.0.0.1:27017";
    public string MongoDatabase { get; init; } = "polestaxi_users";
    public bool Seed { get; init; }

    public static AppConfig FromEnvironment()
    {
        return new AppConfig
        {
            MongoConnection = Environment.GetEnvironmentVariable("MONGO_CONNECTION") ?? "mongodb://127.0.0.1:27017",
            MongoDatabase = Environment.GetEnvironmentVariable("MONGO_USER_DATABASE") ?? "polestaxi_users",
            Seed = string.Equals(Environment.GetEnvironmentVariable("SEED"), "true", StringComparison.OrdinalIgnoreCase)
        };
    }
}