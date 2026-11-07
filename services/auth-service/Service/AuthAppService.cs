using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PolesTaxi.AuthService.Entity.Service;
using PolesTaxi.AuthService.Gateway;
using PolesTaxi.AuthService.Repository;
using PolesTaxi.Shared.Errors;
using PolesTaxi.Shared.Types;

namespace PolesTaxi.AuthService.Service;

public interface IAuthAppService
{
    Task<TokenPair> Login(string login, string password, CancellationToken cancellationToken);
    Task<TokenPair> Refresh(string refreshToken, CancellationToken cancellationToken);
    Task Logout(string accessToken, string refreshToken, CancellationToken cancellationToken);
    Task<UserPrincipal> ValidateToken(string accessToken, CancellationToken cancellationToken);
}

public sealed class AuthAppService : IAuthAppService
{
    private readonly IAuthRepository _tokens;
    private readonly IUserGateway _users;
    private readonly string _secret;
    private readonly TimeSpan _accessTtl;
    private readonly TimeSpan _refreshTtl;

    public AuthAppService(IAuthRepository tokens, IUserGateway users, string secret, TimeSpan accessTtl, TimeSpan refreshTtl)
    {
        _tokens = tokens;
        _users = users;
        _secret = secret;
        _accessTtl = accessTtl;
        _refreshTtl = refreshTtl;
    }

    public async Task<TokenPair> Login(string login, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(login)) throw AppException.Validation("login is required");
        if (string.IsNullOrWhiteSpace(password)) throw AppException.Validation("password is required");

        var principal = await _users.VerifyCredentials(login, password, cancellationToken);
        return await Issue(principal.UserId, principal.Roles, cancellationToken);
    }

    public async Task<TokenPair> Refresh(string refreshToken, CancellationToken cancellationToken)
    {
        var (jti, userId, roles, family) = ReadToken(refreshToken, "refresh");
        var stored = await _tokens.GetRefresh(jti, cancellationToken);
        if (stored is null) throw AppException.Unauthorized("refresh token is not active");

        await _tokens.DeleteRefresh(jti, cancellationToken);
        if (family is not null) await _tokens.DeleteAccess(family, cancellationToken);

        return await Issue(userId, roles, cancellationToken);
    }

    public async Task Logout(string accessToken, string refreshToken, CancellationToken cancellationToken)
    {
        var (accessJti, _, _, _) = ReadToken(accessToken, "access");
        var (refreshJti, _, _, _) = ReadToken(refreshToken, "refresh");
        await _tokens.DeletePair(accessJti, refreshJti, cancellationToken);
    }

    public async Task<UserPrincipal> ValidateToken(string accessToken, CancellationToken cancellationToken)
    {
        var (jti, userId, roles, _) = ReadToken(accessToken, "access");
        var stored = await _tokens.GetAccess(jti, cancellationToken);
        if (stored is null) throw AppException.Unauthorized("access token is not active");

        return new UserPrincipal { UserId = userId, Roles = roles };
    }

    private async Task<TokenPair> Issue(string userId, IReadOnlyList<Role> roles, CancellationToken cancellationToken)
    {
        var accessJti = Guid.NewGuid().ToString("N");
        var refreshJti = Guid.NewGuid().ToString("N");

        var access = WriteToken(userId, roles, accessJti, "access", _accessTtl);
        var refresh = WriteToken(userId, roles, refreshJti, "refresh", _refreshTtl);

        await _tokens.SetAccess(new TokenRecord { Jti = accessJti, UserId = userId, Roles = roles, Family = refreshJti }, _accessTtl, cancellationToken);
        await _tokens.SetRefresh(new TokenRecord { Jti = refreshJti, UserId = userId, Roles = roles, Family = accessJti }, _refreshTtl, cancellationToken);

        return new TokenPair
        {
            AccessToken = access,
            RefreshToken = refresh,
            AccessExpiresInSeconds = (int)_accessTtl.TotalSeconds,
            RefreshExpiresInSeconds = (int)_refreshTtl.TotalSeconds
        };
    }

    private string WriteToken(string userId, IReadOnlyList<Role> roles, string jti, string tokenUse, TimeSpan ttl)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, jti),
            new("token_use", tokenUse)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r.ToWire())));

        var token = new JwtSecurityToken(
            issuer: "polestaxi-auth",
            audience: "polestaxi",
            claims: claims,
            expires: DateTime.UtcNow.Add(ttl),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private (string Jti, string UserId, IReadOnlyList<Role> Roles, string? Family) ReadToken(string token, string expectedUse)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "polestaxi-auth",
                ValidateAudience = true,
                ValidAudience = "polestaxi",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            }, out _);

            var tokenUse = principal.FindFirst("token_use")?.Value;
            if (tokenUse != expectedUse) throw AppException.Unauthorized("invalid token type");

            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
                ?? throw AppException.Unauthorized("jti is missing");
            var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? throw AppException.Unauthorized("sub is missing");

            var roles = principal.FindAll(ClaimTypes.Role).Select(c => EnumMaps.RoleFromWire(c.Value)).ToList();
            return (jti, userId, roles, null);
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            throw AppException.Unauthorized("invalid token");
        }
    }
}