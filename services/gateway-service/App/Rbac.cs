using PolesTaxi.Shared.Types; // Для Role

namespace PolesTaxi.GatewayService.App;

public static class Rbac
{
    public static bool Allowed(string method, string path, List<Role> roles)
    {
        // Аналитика: Analyst, Admin
        if (path.StartsWith("/v1/analytics"))
            return roles.Any(r => r is Role.Analyst or Role.Admin);

        // Профиль водителя: Driver, Admin
        if (path.StartsWith("/v1/drivers"))
            return roles.Any(r => r is Role.Driver or Role.Admin);

        // Кошелёк: User, Driver, Admin
        if (path.StartsWith("/v1/wallets"))
            return roles.Any(r => r is Role.User or Role.Driver or Role.Admin);

        // Назначение ролей: только Admin
        if (method == "POST" && path.StartsWith("/v1/users/") && path.EndsWith("/roles"))
            return roles.Contains(Role.Admin);

        // Создание заказа: User, Admin
        if (method == "POST" && path == "/v1/orders")
            return roles.Any(r => r is Role.User or Role.Admin);

        // Действия с поездкой (accept, decline, start, complete): Driver, Admin
        if (path.StartsWith("/v1/orders/") && (path.EndsWith("/accept") || path.EndsWith("/decline") || path.EndsWith("/start") || path.EndsWith("/complete")))
            return roles.Any(r => r is Role.Driver or Role.Admin);

        // Чтение и отмена заказа, GraphQL: все роли
        if (path.StartsWith("/v1/orders") || path.StartsWith("/graphql"))
            return roles.Any(r => r is Role.User or Role.Driver or Role.Analyst or Role.Admin);

        // По умолчанию — запрещено (или разрешено, если не указано иное)
        return false; 
    }
}