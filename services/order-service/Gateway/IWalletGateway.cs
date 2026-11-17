namespace PolesTaxi.OrderService.Gateway;

public interface IWalletGateway
{
    Task Debit(string userId, string orderId, decimal amount, CancellationToken cancellationToken);
    Task Refund(string userId, string orderId, decimal amount, CancellationToken cancellationToken);
}