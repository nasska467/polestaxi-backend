namespace PolesTaxi.UserService.Gateway;

public interface IWalletGateway
{
    Task CreateWallet(string userId, CancellationToken cancellationToken);
}