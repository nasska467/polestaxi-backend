namespace PolesTaxi.GatewayService.App;

public static class Rbac
{
    public static bool Allowed(string method, string path, IReadOnlyList<string> roles)
    {
        // Анонимные пути (проверяются до RBAC)
        if (path.StartsWith("/v1/auth/") || path.StartsWith("/v1/users/register") || path == "/healthz")
            return true;

        // Аналитика — только Analyst и Admin
        if (path.StartsWith("/v1/analytics"))
            return roles.Contains("Analyst") || roles.Contains("Admin");

        // Управление ролями — только Admin
        if (path.StartsWith("/v1/users/") && path.EndsWith("/roles"))
            return roles.Contains("Admin");

        // Профиль водителя — Driver и Admin
        if (path.StartsWith("/v1/drivers"))
            return roles.Contains("Driver") || roles.Contains("Admin");

        // Кошелек — User, Driver, Admin
        if (path.StartsWith("/v1/wallets"))
            return roles.Contains("User") || roles.Contains("Driver") || roles.Contains("Admin");

        // Заказы и GraphQL — User, Driver, Analyst, Admin
        if (path.StartsWith("/v1/orders") || path.StartsWith("/graphql"))
            return roles.Contains("User") || roles.Contains("Driver") || roles.Contains("Analyst") || roles.Contains("Admin");

        // Профиль пользователя — все авторизованные
        if (path.StartsWith("/v1/users/me"))
            return roles.Count > 0;

        return false;
    }
}