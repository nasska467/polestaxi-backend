using Grpc.Core;
using PolesTaxi.Contracts.User.V1;
using PolesTaxi.Shared.Hosting;
using PolesTaxi.UserService.Entity.Service;
using PolesTaxi.UserService.Service;

namespace PolesTaxi.UserService.Handler.Grpc;

public sealed class UserGrpcHandler : PolesTaxi.Contracts.User.V1.UserService.UserServiceBase
{
    private readonly IUserAppService _service;

    public UserGrpcHandler(IUserAppService service) => _service = service;

    public override async Task<UserResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        try
        {
            var user = await _service.Register(
                request.Name,
                request.Phone,
                request.Email,
                request.Password,
                (PolesTaxi.Shared.Types.Role)(int)request.Role,
                context.CancellationToken);

            return ToResponse(user);
        }
        catch (Exception ex)
        {
            // Возвращаем текст ошибки, чтобы увидеть причину
            throw new RpcException(new Status(StatusCode.Internal, ex.Message + " | " + ex.StackTrace));
        }
    }

    public override async Task<UserResponse> GetMe(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
    {
        var userId = CallContext.RequireUserId(context);
        var user = await _service.GetMe(userId, context.CancellationToken);
        return ToResponse(user);
    }

    private static UserResponse ToResponse(User user)
    {
        var response = new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Phone = user.Phone,
            Email = user.Email,
            Rating = user.Rating,
            IsDeleted = user.IsDeleted
        };
        response.Roles.AddRange(user.Roles.Select(r => (PolesTaxi.Contracts.User.V1.Role)(int)r));
        return response;
    }
}