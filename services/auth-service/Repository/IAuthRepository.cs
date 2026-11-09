using PolesTaxi.AuthService.Entity.Service;

namespace PolesTaxi.AuthService.Repository;

public interface IAuthRepository
{
    Task SetAccess(TokenRecord token, TimeSpan ttl, CancellationToken cancellationToken);
    Task<TokenRecord?> GetAccess(string jti, CancellationToken cancellationToken);
    Task DeleteAccess(string jti, CancellationToken cancellationToken);
    Task SetRefresh(TokenRecord token, TimeSpan ttl, CancellationToken cancellationToken);
    Task<TokenRecord?> GetRefresh(string jti, CancellationToken cancellationToken);
    Task DeleteRefresh(string jti, CancellationToken cancellationToken);
    Task DeletePair(string accessJti, string refreshJti, CancellationToken cancellationToken);
}