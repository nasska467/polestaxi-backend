using Grpc.Core;
using PolesTaxi.Shared.Errors;

namespace PolesTaxi.Shared.Hosting;

public static class CallContext
{
    public static string RequireUserId(ServerCallContext context)
    {
        var userId = context.RequestHeaders.GetValue("x-user-id");
        if (string.IsNullOrWhiteSpace(userId))
            throw AppException.Unauthorized("x-user-id header is missing");
        return userId;
    }

    public static string? GetRoles(ServerCallContext context)
    {
        return context.RequestHeaders.GetValue("x-user-roles");
    }
}