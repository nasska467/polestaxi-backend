using PolesTaxi.Shared.Errors;
using PolesTaxi.Shared.Types;
using PolesTaxi.UserService.Entity.Service;
using PolesTaxi.UserService.Gateway;
using PolesTaxi.UserService.Repository;

namespace PolesTaxi.UserService.Service;

public interface IUserAppService
{
    Task<User> Register(string name, string phone, string email, string password, Role role, CancellationToken cancellationToken);
    Task<User> GetMe(string userId, CancellationToken cancellationToken);
    Task<User> GetByEmailOrPhone(string login, CancellationToken cancellationToken);
}

public sealed class UserAppService : IUserAppService
{
    private readonly IUserRepository _users;
    private readonly IDriverGateway _drivers;
    private readonly IWalletGateway _wallets;

    public UserAppService(IUserRepository users, IDriverGateway drivers, IWalletGateway wallets)
    {
        _users = users;
        _drivers = drivers;
        _wallets = wallets;
    }

    public async Task<User> Register(string name, string phone, string email, string password, Role role, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[Register] Start: name={name}, email={email}, role={role}");

        if (role != Role.User && role != Role.Driver)
            throw AppException.Validation("role must be User or Driver");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        
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

        // 1. Создаем пользователя в БД
        var result = await _users.Create(user, cancellationToken);
        Console.WriteLine($"[Register] User created: id={result.Id}");

        try
        {
            // 2. Создаем кошелек
            Console.WriteLine($"[Register] Creating wallet...");
            await _wallets.CreateWallet(result.Id, cancellationToken);
            Console.WriteLine($"[Register] Wallet created");

            // 3. Если роль Driver — создаем анкету водителя
            if (role == Role.Driver)
            {
                Console.WriteLine($"[Register] Calling Driver Service...");
                await _drivers.RegisterDriver(result.Id, cancellationToken);
                Console.WriteLine($"[Register] Driver created");
            }
        }
        catch (Exception ex)
        {
            // 4. КОМПЕНСАЦИЯ: если что-то пошло не так, помечаем пользователя как удаленного
            Console.WriteLine($"[Register] Error: {ex.Message}. Rolling back user creation...");
            
            try
            {
                // Используем SoftDelete, который уже есть в репозитории
                await _users.SoftDelete(result.Id, cancellationToken);
                Console.WriteLine($"[Register] User {result.Id} soft-deleted (compensation)");
            }
            catch (Exception deleteEx)
            {
                // Если удалить не удалось — логируем, но не скрываем исходную ошибку
                Console.Error.WriteLine($"[Register] Failed to soft-delete user {result.Id}: {deleteEx.Message}");
            }

            // Пробрасываем исходную ошибку клиенту
            throw;
        }

        return result;
    }

    public async Task<User> GetMe(string userId, CancellationToken cancellationToken)
    {
        return await _users.GetById(userId, cancellationToken);
    }

    public async Task<User> GetByEmailOrPhone(string login, CancellationToken cancellationToken)
    {
        try
        {
            return await _users.GetByEmail(login, cancellationToken);
        }
        catch (AppException ex) when (ex.Code == ErrorCodes.NotFound)
        {
            return await _users.GetByPhone(login, cancellationToken);
        }
    }
}