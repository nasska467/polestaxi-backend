using PolesTaxi.Shared.Types;

namespace PolesTaxi.AuthService.Entity.Service;

public sealed class TokenRecord
{
    public required string Jti { get; init; }
    public required string UserId { get; init; }
    public required IReadOnlyList<Role> Roles { get; init; }
    public string? Family { get; init; }
}

public sealed class TokenPair
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public int AccessExpiresInSeconds { get; init; }
    public int RefreshExpiresInSeconds { get; init; }
}

public sealed class UserPrincipal
{
    public required string UserId { get; init; }
    public required IReadOnlyList<Role> Roles { get; init; }
}