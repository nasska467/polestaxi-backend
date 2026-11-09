namespace PolesTaxi.UserService.Gateway;

public interface IDriverGateway
{
    Task RegisterDriver(string userId, CancellationToken cancellationToken);
}