using PolesTaxi.AuthService.Entity.Service;

namespace PolesTaxi.AuthService.Gateway;

public interface IUserGateway
{
    Task<UserPrincipal> VerifyCredentials(string login, string password, CancellationToken cancellationToken);
}