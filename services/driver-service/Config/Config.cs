namespace PolesTaxi.DriverService.Config;

public sealed class AppConfig
{
    public string MongoConnection { get; init; } = "mongodb://127.0.0.1:27017";
    public string MongoDatabase { get; init; } = "polestaxi_drivers";

    public static AppConfig FromEnvironment()
    {
        return new AppConfig
        {
            MongoConnection = Environment.GetEnvironmentVariable("MONGO_CONNECTION") ?? "mongodb://127.0.0.1:27017",
            MongoDatabase = Environment.GetEnvironmentVariable("MONGO_DRIVER_DATABASE") ?? "polestaxi_drivers"
        };
    }
}