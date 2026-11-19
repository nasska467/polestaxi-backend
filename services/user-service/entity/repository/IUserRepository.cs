using PolesTaxi.UserService.Entity.Service;

namespace PolesTaxi.UserService.Repository;

public interface IUserRepository
{
    Task<User> Create(User user, CancellationToken cancellationToken);
    Task<User> GetById(string id, CancellationToken cancellationToken);
    Task<User> GetByEmail(string email, CancellationToken cancellationToken);
    Task SoftDelete(string id, CancellationToken cancellationToken);
}