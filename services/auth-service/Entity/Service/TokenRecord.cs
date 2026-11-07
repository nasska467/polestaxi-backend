using PolesTaxi.Shared.Types;

namespace PolesTaxi.AuthService.Entity.Service;

public sealed class TokenRecord
{
    public required string Jti { get; init; }
    public required string UserId { get; init; }
    public required IReadOnlyList<Role> Roles { get; init; }
    public string? Family { get; init; }
}