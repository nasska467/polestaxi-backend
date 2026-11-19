namespace PolesTaxi.DriverService.Config;

public sealed class AppConfig
{
    public string HttpAddr { get; init; } = "http://0.0.0.0:8083";
    public string GrpcAddr { get; init; } = "http://0.0.0.0:9083";
    public string MongoConnection { get; init; } = "mongodb://127.0.0.1:27018";
    public string MongoDatabase { get; init; } = "polestaxi_drivers";

    public static AppConfig FromEnvironment()
    {
        return new AppConfig
        {
            HttpAddr = Environment.GetEnvironmentVariable("HTTP_ADDR") ?? "http://0.0.0.0:8083",
            GrpcAddr = Environment.GetEnvironmentVariable("GRPC_ADDR") ?? "http://0.0.0.0:9083",
            MongoConnection = Environment.GetEnvironmentVariable("MONGO_CONNECTION") ?? "mongodb://127.0.0.1:27018",
            MongoDatabase = Environment.GetEnvironmentVariable("MONGO_DRIVER_DATABASE") ?? "polestaxi_drivers"
        };
    }
}