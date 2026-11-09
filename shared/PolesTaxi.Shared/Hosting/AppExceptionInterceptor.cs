using Grpc.Core;
using Grpc.Core.Interceptors;
using PolesTaxi.Shared.Errors;

namespace PolesTaxi.Shared.Hosting;

public sealed class AppExceptionInterceptor : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (AppException ex)
        {
            throw new RpcException(new Status(ToStatusCode(ex.Code), ex.Message));
        }
    }

    private static StatusCode ToStatusCode(string code) => code switch
    {
        ErrorCodes.Validation => StatusCode.InvalidArgument,
        ErrorCodes.Unauthorized => StatusCode.Unauthenticated,
        ErrorCodes.Forbidden => StatusCode.PermissionDenied,
        ErrorCodes.NotFound => StatusCode.NotFound,
        ErrorCodes.Conflict => StatusCode.AlreadyExists,
        _ => StatusCode.Internal
    };
}