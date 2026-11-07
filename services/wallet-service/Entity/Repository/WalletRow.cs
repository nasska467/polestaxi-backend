namespace PolesTaxi.WalletService.Entity.Repository;

public sealed class WalletRow
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class TransactionRow
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}