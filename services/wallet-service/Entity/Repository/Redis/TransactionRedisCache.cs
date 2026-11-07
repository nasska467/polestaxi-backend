using System.Text.Json;
using PolesTaxi.WalletService.Entity.Service;
using StackExchange.Redis;

namespace PolesTaxi.WalletService.Repository.Redis;

public sealed class TransactionRedisCache
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly IDatabase _db;
    private readonly TimeSpan _ttl;

    public TransactionRedisCache(IDatabase db, TimeSpan ttl)
    {
        _db = db;
        _ttl = ttl;
    }

    public async Task Set(string userId, IReadOnlyList<WalletTransaction> transactions, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.StringSetAsync($"tx:{userId}", JsonSerializer.Serialize(transactions, Json), _ttl);
    }

    public async Task<IReadOnlyList<WalletTransaction>?> Get(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await _db.StringGetAsync($"tx:{userId}");
        if (value.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<List<WalletTransaction>>(value!, Json);
    }

    public async Task Invalidate(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.KeyDeleteAsync($"tx:{userId}");
    }
}