namespace PolesTaxi.WalletService.Entity.Service;

public enum WalletTxType { Unspecified = 0, Debit = 1, Credit = 2, Refund = 3 }
public enum WalletTxStatus { Unspecified = 0, Pending = 1, Completed = 2, Failed = 3, RolledBack = 4 }

public sealed class Wallet
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public decimal Balance { get; init; }
    public required string Currency { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class WalletTransaction
{
    public required string Id { get; init; }
    public required string WalletId { get; init; }
    public WalletTxType Type { get; init; }
    public decimal Amount { get; init; }
    public WalletTxStatus Status { get; init; }
    public string? OrderId { get; init; }
    public DateTime CreatedAt { get; init; }
}