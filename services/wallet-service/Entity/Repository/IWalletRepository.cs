using PolesTaxi.WalletService.Entity.Service;

namespace PolesTaxi.WalletService.Repository;

public interface IWalletRepository
{
    Task<Wallet> CreateWallet(string userId, string currency, CancellationToken cancellationToken);
    Task<Wallet> GetByUserId(string userId, CancellationToken cancellationToken);
    Task<WalletTransaction> ApplyMoney(string userId, WalletTxType type, decimal amount, string? orderId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WalletTransaction>> ListTransactions(string userId, int limit, CancellationToken cancellationToken);
}