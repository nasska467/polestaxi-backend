using PolesTaxi.Shared.Errors;
using PolesTaxi.WalletService.Entity.Service;
using PolesTaxi.WalletService.Repository;

namespace PolesTaxi.WalletService.Service;

public sealed class WalletAppService : IWalletAppService
{
    private readonly IWalletRepository _wallets;
    private readonly decimal _signupBonus;

    public WalletAppService(IWalletRepository wallets, decimal signupBonus = 5000m)
    {
        _wallets = wallets;
        _signupBonus = signupBonus;
    }

    public async Task<Wallet> CreateWallet(string userId, string currency, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw AppException.Validation("user_id cannot be empty");

        var code = string.IsNullOrWhiteSpace(currency) ? "RUB" : currency.Trim().ToUpperInvariant();
        
        var wallet = await _wallets.CreateWallet(userId, code, cancellationToken);
        
        if (_signupBonus > 0)
        {
            await _wallets.ApplyMoney(userId, WalletTxType.Credit, _signupBonus, null, $"signup:{userId}", cancellationToken);
            return await _wallets.GetByUserId(userId, cancellationToken);
        }

        return wallet;
    }

    public async Task<Wallet> GetByUserId(string userId, CancellationToken cancellationToken)
    {
        return await _wallets.GetByUserId(userId, cancellationToken);
    }

    public async Task<WalletTransaction> ApplyMoney(string userId, WalletTxType type, decimal amount, string? orderId, string? idempotencyKey, CancellationToken cancellationToken)
    {
        return await _wallets.ApplyMoney(userId, type, amount, orderId, idempotencyKey, cancellationToken);
    }

    // ДОБАВЛЕНО: Метод для списания (Debit)
    public async Task<WalletTransaction> Debit(string userId, string? orderId, decimal amount, string? idempotencyKey, CancellationToken cancellationToken)
    {
        return await _wallets.ApplyMoney(userId, WalletTxType.Debit, amount, orderId, idempotencyKey, cancellationToken);
    }

    // ДОБАВЛЕНО: Метод для зачисления/возврата (Credit)
    public async Task<WalletTransaction> Credit(string userId, string? orderId, decimal amount, bool isRefund, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var type = isRefund ? WalletTxType.Refund : WalletTxType.Credit;
        return await _wallets.ApplyMoney(userId, type, amount, orderId, idempotencyKey, cancellationToken);
    }
}