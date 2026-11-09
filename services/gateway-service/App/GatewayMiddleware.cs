using System.Net;
using Grpc.Core;
using PolesTaxi.Contracts.Auth.V1;
using PolesTaxi.Shared.Errors;

namespace PolesTaxi.GatewayService.App;

public sealed class GatewayMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthService.AuthServiceClient _authClient;

    public GatewayMiddleware(RequestDelegate next, AuthService.AuthServiceClient authClient)
    {
        _next = next;
        _authClient = authClient;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsAnonymousPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new ErrorBody("UNAUTHORIZED", "unauthorized"));
            return;
        }

        var token = authHeader.Replace("Bearer ", string.Empty);
        try
        {
            var response = await _authClient.ValidateTokenAsync(new ValidateTokenRequest { AccessToken = token });
            var roles = response.Roles.ToList();

            // Проверка RBAC
            if (!Rbac.Allowed(context.Request.Method, context.Request.Path, roles))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new ErrorBody("FORBIDDEN", "role not allowed"));
                return;
            }

            context.Request.Headers["x-user-id"] = response.UserId;
            context.Request.Headers["x-user-roles"] = string.Join(",", roles);
            await _next(context);
        }
        catch (RpcException)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new ErrorBody("UNAUTHORIZED", "unauthorized"));
        }
    }

    private static bool IsAnonymousPath(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return value.StartsWith("/v1/auth/login", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/v1/auth/refresh", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/v1/users/register", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/healthz", StringComparison.OrdinalIgnoreCase);
    }
}