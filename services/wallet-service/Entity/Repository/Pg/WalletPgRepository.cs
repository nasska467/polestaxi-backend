using Dapper;
using Npgsql;
using PolesTaxi.Shared.Errors;
using PolesTaxi.WalletService.Entity.Repository;
using PolesTaxi.WalletService.Entity.Service;

namespace PolesTaxi.WalletService.Repository.Pg;

public sealed class WalletPgRepository : IWalletRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public WalletPgRepository(NpgsqlDataSource dataSource, object? cache = null)
    {
        _dataSource = dataSource;
    }

    public async Task<Wallet> CreateWallet(string userId, string currency, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        try
        {
            var row = await conn.QuerySingleAsync<WalletRow>(@"
                INSERT INTO wallets (id, user_id, balance, currency)
                VALUES (@Id, @UserId, 0, @Currency)
                RETURNING id AS Id, user_id AS UserId, balance AS Balance, currency AS Currency, created_at AS CreatedAt",
                new { Id = Guid.NewGuid(), UserId = userId, Currency = currency });
            
            return ToWallet(row);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            throw AppException.Conflict("wallet already exists for user");
        }
    }

    public async Task<Wallet> GetByUserId(string userId, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var row = await conn.QuerySingleOrDefaultAsync<WalletRow>(
            "SELECT id AS Id, user_id AS UserId, balance AS Balance, currency AS Currency, created_at AS CreatedAt FROM wallets WHERE user_id = @userId",
            new { userId });
        
        if (row is null) throw AppException.NotFound("wallet not found");
        return ToWallet(row);
    }

    public async Task<WalletTransaction> ApplyMoney(string userId, WalletTxType type, decimal amount, string? orderId, CancellationToken cancellationToken)
    {
        if (amount <= 0) throw AppException.Validation("amount must be positive");

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            var wallet = await conn.QuerySingleOrDefaultAsync<WalletRow>(
                "SELECT id AS Id, user_id AS UserId, balance AS Balance, currency AS Currency, created_at AS CreatedAt FROM wallets WHERE user_id = @userId FOR UPDATE",
                new { userId }, tx);

            if (wallet is null) throw AppException.NotFound("wallet not found");

            var delta = type == WalletTxType.Debit ? -amount : amount;
            var newBalance = wallet.Balance + delta;

            if (newBalance < 0) throw AppException.Conflict("insufficient funds");

            await conn.ExecuteAsync("UPDATE wallets SET balance = @balance WHERE id = @id", new { balance = newBalance, id = wallet.Id }, tx);

            var row = await conn.QuerySingleAsync<TransactionRow>(@"
                INSERT INTO transactions (id, wallet_id, type, amount, status, order_id)
                VALUES (@Id, @WalletId, @Type, @Amount, 'completed', @OrderId)
                RETURNING id AS Id, wallet_id AS WalletId, type AS Type, amount AS Amount, status AS Status, order_id AS OrderId, created_at AS CreatedAt",
                new { Id = Guid.NewGuid(), WalletId = wallet.Id, Type = TypeToWire(type), Amount = amount, OrderId = orderId }, tx);

            await tx.CommitAsync(cancellationToken);
            return ToTx(row);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<WalletTransaction>> ListTransactions(string userId, int limit, CancellationToken cancellationToken)
    {
        var take = limit <= 0 ? 20 : Math.Min(limit, 100);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);

        var rows = await conn.QueryAsync<TransactionRow>(@"
            SELECT t.id AS Id, t.wallet_id AS WalletId, t.type AS Type, t.amount AS Amount, t.status AS Status, t.order_id AS OrderId, t.created_at AS CreatedAt
            FROM transactions t
            JOIN wallets w ON w.id = t.wallet_id
            WHERE w.user_id = @userId
            ORDER BY t.created_at DESC
            LIMIT @take", new { userId, take });

        return rows.Select(ToTx).ToList();
    }

    private static Wallet ToWallet(WalletRow row) => new()
    {
        Id = row.Id.ToString(),
        UserId = row.UserId,
        Balance = row.Balance,
        Currency = row.Currency.Trim(),
        CreatedAt = row.CreatedAt
    };

    private static WalletTransaction ToTx(TransactionRow row) => new()
    {
        Id = row.Id.ToString(),
        WalletId = row.WalletId.ToString(),
        Type = TypeFromWire(row.Type),
        Amount = row.Amount,
        Status = StatusFromWire(row.Status),
        OrderId = row.OrderId,
        CreatedAt = row.CreatedAt
    };

    private static string TypeToWire(WalletTxType type) => type switch { WalletTxType.Debit => "debit", WalletTxType.Credit => "credit", WalletTxType.Refund => "refund", _ => "unspecified" };
    private static WalletTxType TypeFromWire(string v) => v switch { "debit" => WalletTxType.Debit, "credit" => WalletTxType.Credit, "refund" => WalletTxType.Refund, _ => WalletTxType.Unspecified };
    private static WalletTxStatus StatusFromWire(string v) => v switch { "pending" => WalletTxStatus.Pending, "completed" => WalletTxStatus.Completed, "failed" => WalletTxStatus.Failed, "rolled_back" => WalletTxStatus.RolledBack, _ => WalletTxStatus.Unspecified };
}