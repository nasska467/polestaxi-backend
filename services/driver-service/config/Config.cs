namespace PolesTaxi.DriverService.Config;

public sealed class Config
{
    public string HttpAddr { get; init; } = "http://localhost:8080";
    public string GrpcAddr { get; init; } = "localhost:9090";

    public static Config FromEnvironment()
    {
        return new Config
        {
            HttpAddr = Environment.GetEnvironmentVariable("HTTP_ADDR") ?? "http://localhost:8080",
            GrpcAddr = Environment.GetEnvironmentVariable("GRPC_ADDR") ?? "localhost:9090",
        };
    }
}
