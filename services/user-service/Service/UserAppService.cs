using PolesTaxi.Shared.Errors;
using PolesTaxi.Shared.Types;
using PolesTaxi.UserService.Entity.Service;
using PolesTaxi.UserService.Repository;

namespace PolesTaxi.UserService.Service;

public interface IUserAppService
{
    Task<User> Register(string name, string phone, string email, string password, Role role, CancellationToken cancellationToken);
    Task<User> GetMe(string userId, CancellationToken cancellationToken);
}

public sealed class UserAppService : IUserAppService
{
    private readonly IUserRepository _users;

    public UserAppService(IUserRepository users) => _users = users;

    public async Task<User> Register(string name, string phone, string email, string password, Role role, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[Register] Start: name={name}, email={email}, role={role}");

        if (role != Role.User && role != Role.Driver)
            throw AppException.Validation("role must be User or Driver");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        Console.WriteLine($"[Register] Password hashed");

        var user = new User
        {
            Id = string.Empty,
            Name = name,
            Phone = phone,
            Email = email,
            PasswordHash = passwordHash,
            Roles = new[] { role },
            Rating = 0,
            RecentRatings = Array.Empty<double>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Console.WriteLine($"[Register] Calling repository...");
        var result = await _users.Create(user, cancellationToken);
        Console.WriteLine($"[Register] Success: id={result.Id}");
        return result;
    }

    public async Task<User> GetMe(string userId, CancellationToken cancellationToken)
    {
        return await _users.GetById(userId, cancellationToken);
    }
}