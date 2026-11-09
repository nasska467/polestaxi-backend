namespace PolesTaxi.GatewayService.Config;

public sealed class AppConfig
{
    public string HttpAddr { get; init; } = "http://0.0.0.0:8080";
    public string AuthGrpcAddr { get; init; } = "http://127.0.0.1:9082";
    public string UserHttp { get; init; } = "http://127.0.0.1:8081/";
    public string AuthHttp { get; init; } = "http://127.0.0.1:8082/";
    public string DriverHttp { get; init; } = "http://127.0.0.1:8083/";
    public string OrderHttp { get; init; } = "http://127.0.0.1:8084/";
    public string WalletHttp { get; init; } = "http://127.0.0.1:8085/";
    public string AnalyticHttp { get; init; } = "http://127.0.0.1:8086/";

    public static AppConfig FromEnvironment()
    {
        return new AppConfig
        {
            HttpAddr = Environment.GetEnvironmentVariable("HTTP_ADDR") ?? "http://0.0.0.0:8080",
            AuthGrpcAddr = Environment.GetEnvironmentVariable("AUTH_GRPC_ADDR") ?? "http://127.0.0.1:9082",
            UserHttp = Environment.GetEnvironmentVariable("USER_HTTP") ?? "http://127.0.0.1:8081/",
            AuthHttp = Environment.GetEnvironmentVariable("AUTH_HTTP") ?? "http://127.0.0.1:8082/",
            DriverHttp = Environment.GetEnvironmentVariable("DRIVER_HTTP") ?? "http://127.0.0.1:8083/",
            OrderHttp = Environment.GetEnvironmentVariable("ORDER_HTTP") ?? "http://127.0.0.1:8084/",
            WalletHttp = Environment.GetEnvironmentVariable("WALLET_HTTP") ?? "http://127.0.0.1:8085/",
            AnalyticHttp = Environment.GetEnvironmentVariable("ANALYTIC_HTTP") ?? "http://127.0.0.1:8086/"
        };
    }
}