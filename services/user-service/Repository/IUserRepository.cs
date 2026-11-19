using PolesTaxi.UserService.Entity.Service;

namespace PolesTaxi.UserService.Repository;

public interface IUserRepository
{
    Task<User> Create(User user, CancellationToken cancellationToken);
    Task<User> GetById(string id, CancellationToken cancellationToken);
    Task<User> GetByEmail(string email, CancellationToken cancellationToken);
    Task<User> GetByPhone(string phone, CancellationToken cancellationToken); // <-- добавили
    Task SoftDelete(string id, CancellationToken cancellationToken);
}