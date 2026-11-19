using Grpc.Core;
using PolesTaxi.AuthService.Entity.Service;
using PolesTaxi.Contracts.User.V1;
using PolesTaxi.Shared.Errors;
using PolesTaxi.Shared.Types;

namespace PolesTaxi.AuthService.Gateway.Grpc;

public sealed class UserGrpcGateway : IUserGateway
{
    private readonly UserService.UserServiceClient _client;

    public UserGrpcGateway(UserService.UserServiceClient client) => _client = client;

    public async Task<UserPrincipal> VerifyCredentials(string login, string password, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.VerifyCredentialsAsync(new VerifyCredentialsRequest
            {
                Login = login,
                Password = password
            }, cancellationToken: cancellationToken);

            return new UserPrincipal
            {
                UserId = response.UserId,
                // Явно приводим к нашему Role, чтобы не было неоднозначности
                Roles = response.Roles.Select(r => (PolesTaxi.Shared.Types.Role)(int)r)
                    .Where(r => r != PolesTaxi.Shared.Types.Role.Unspecified)
                    .ToList()
            };
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unauthenticated or StatusCode.NotFound)
        {
            throw AppException.Unauthorized("invalid credentials");
        }
    }
}