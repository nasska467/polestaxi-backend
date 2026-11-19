using Yarp.ReverseProxy.Configuration;

namespace PolesTaxi.GatewayService.App;

public static class YarpConfig
{
    public static IReadOnlyList<RouteConfig> Routes(Config.AppConfig config) => new[]
    {
        new RouteConfig { RouteId = "auth", ClusterId = "auth", Match = new RouteMatch { Path = "/v1/auth/{**catch-all}" } },
        new RouteConfig { RouteId = "users", ClusterId = "user", Match = new RouteMatch { Path = "/v1/users/{**catch-all}" } },
        new RouteConfig { RouteId = "drivers", ClusterId = "driver", Match = new RouteMatch { Path = "/v1/drivers/{**catch-all}" } },
        new RouteConfig { RouteId = "orders", ClusterId = "order", Match = new RouteMatch { Path = "/v1/orders/{**catch-all}" } },
        new RouteConfig { RouteId = "graphql", ClusterId = "order", Match = new RouteMatch { Path = "/graphql" } },
        new RouteConfig { RouteId = "wallets", ClusterId = "wallet", Match = new RouteMatch { Path = "/v1/wallets/{**catch-all}" } },
        new RouteConfig { RouteId = "analytics", ClusterId = "analytic", Match = new RouteMatch { Path = "/v1/analytics/{**catch-all}" } }
    };

    public static IReadOnlyList<ClusterConfig> Clusters(Config.AppConfig config) => new[]
    {
        new ClusterConfig { ClusterId = "auth", Destinations = new Dictionary<string, DestinationConfig> { ["auth"] = new() { Address = config.AuthHttp } } },
        new ClusterConfig { ClusterId = "user", Destinations = new Dictionary<string, DestinationConfig> { ["user"] = new() { Address = config.UserHttp } } },
        new ClusterConfig { ClusterId = "driver", Destinations = new Dictionary<string, DestinationConfig> { ["driver"] = new() { Address = config.DriverHttp } } },
        new ClusterConfig { ClusterId = "order", Destinations = new Dictionary<string, DestinationConfig> { ["order"] = new() { Address = config.OrderHttp } } },
        new ClusterConfig { ClusterId = "wallet", Destinations = new Dictionary<string, DestinationConfig> { ["wallet"] = new() { Address = config.WalletHttp } } },
        new ClusterConfig { ClusterId = "analytic", Destinations = new Dictionary<string, DestinationConfig> { ["analytic"] = new() { Address = config.AnalyticHttp } } }
    };
}