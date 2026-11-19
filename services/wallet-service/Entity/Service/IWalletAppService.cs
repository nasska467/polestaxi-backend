using PolesTaxi.WalletService.Entity.Service;

namespace PolesTaxi.WalletService.Service;

public interface IWalletAppService
{
    Task<Wallet> CreateWallet(string userId, string currency, CancellationToken cancellationToken);
    Task<Wallet> GetByUserId(string userId, CancellationToken cancellationToken);
    
    // ДОБАВИТЬ ЭТИ ДВА МЕТОДА:
    Task<WalletTransaction> Debit(string userId, string? orderId, decimal amount, string? idempotencyKey, CancellationToken cancellationToken);
    Task<WalletTransaction> Credit(string userId, string? orderId, decimal amount, bool isRefund, string? idempotencyKey, CancellationToken cancellationToken);

    // Этот метод оставляем (он уже есть):
    Task<WalletTransaction> ApplyMoney(string userId, WalletTxType type, decimal amount, string? orderId, string? idempotencyKey, CancellationToken cancellationToken);
}