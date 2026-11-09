using PolesTaxi.WalletService.Entity.Service;
using PolesTaxi.WalletService.Repository;

namespace PolesTaxi.WalletService.Service;

public interface IWalletAppService
{
    Task<Wallet> CreateWallet(string userId, string currency, CancellationToken cancellationToken);
    Task<Wallet> GetByUserId(string userId, CancellationToken cancellationToken);
    Task<WalletTransaction> ApplyMoney(string userId, WalletTxType type, decimal amount, string? orderId, CancellationToken cancellationToken);
}

public sealed class WalletAppService : IWalletAppService
{
    private readonly IWalletRepository _wallets;

    public WalletAppService(IWalletRepository wallets) => _wallets = wallets;

    public async Task<Wallet> CreateWallet(string userId, string currency, CancellationToken cancellationToken)
    {
        var code = string.IsNullOrWhiteSpace(currency) ? "RUB" : currency.Trim().ToUpperInvariant();
        return await _wallets.CreateWallet(userId, code, cancellationToken);
    }

    public async Task<Wallet> GetByUserId(string userId, CancellationToken cancellationToken)
    {
        return await _wallets.GetByUserId(userId, cancellationToken);
    }

    public async Task<WalletTransaction> ApplyMoney(string userId, WalletTxType type, decimal amount, string? orderId, CancellationToken cancellationToken)
    {
        return await _wallets.ApplyMoney(userId, type, amount, orderId, cancellationToken);
    }
}