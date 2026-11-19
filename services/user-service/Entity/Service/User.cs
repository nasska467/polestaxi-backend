using PolesTaxi.Shared.Types;

namespace PolesTaxi.UserService.Entity.Service;

public sealed class User
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Phone { get; init; }
    public required string Email { get; init; }
    public required string PasswordHash { get; init; }
    public required IReadOnlyList<Role> Roles { get; init; }
    public double Rating { get; init; }
    public required IReadOnlyList<double> RecentRatings { get; init; }
    public bool IsDeleted { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}