using System.Text.Json;
using PolesTaxi.AuthService.Entity.Repository;
using PolesTaxi.AuthService.Entity.Service;
using PolesTaxi.Shared.Types;
using StackExchange.Redis;

namespace PolesTaxi.AuthService.Repository.Redis;

public sealed class AuthRedisRepository : IAuthRepository
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly IDatabase _db;

    public AuthRedisRepository(IDatabase db) => _db = db;

    public Task SetAccess(TokenRecord token, TimeSpan ttl, CancellationToken cancellationToken) => Set("access", token, ttl, cancellationToken);
    public Task<TokenRecord?> GetAccess(string jti, CancellationToken cancellationToken) => Get("access", jti, cancellationToken);
    public Task DeleteAccess(string jti, CancellationToken cancellationToken) => Delete("access", jti, cancellationToken);
    public Task SetRefresh(TokenRecord token, TimeSpan ttl, CancellationToken cancellationToken) => Set("refresh", token, ttl, cancellationToken);
    public Task<TokenRecord?> GetRefresh(string jti, CancellationToken cancellationToken) => Get("refresh", jti, cancellationToken);
    public Task DeleteRefresh(string jti, CancellationToken cancellationToken) => Delete("refresh", jti, cancellationToken);

    public async Task DeletePair(string accessJti, string refreshJti, CancellationToken cancellationToken)
    {
        await DeleteAccess(accessJti, cancellationToken);
        await DeleteRefresh(refreshJti, cancellationToken);
    }

    private async Task Set(string kind, TokenRecord token, TimeSpan ttl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var doc = new TokenDocument
        {
            UserId = token.UserId,
            Roles = token.Roles.Select(r => r.ToWire()).ToList(),
            Family = token.Family
        };
        await _db.StringSetAsync($"{kind}:{token.Jti}", JsonSerializer.Serialize(doc, Json), ttl);
    }

    private async Task<TokenRecord?> Get(string kind, string jti, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await _db.StringGetAsync($"{kind}:{jti}");
        if (value.IsNullOrEmpty) return null;

        var doc = JsonSerializer.Deserialize<TokenDocument>(value!, Json);
        if (doc is null) return null;

        return new TokenRecord
        {
            Jti = jti,
            UserId = doc.UserId,
            Roles = doc.Roles.Select(EnumMaps.RoleFromWire).ToList(),
            Family = doc.Family
        };
    }

    private async Task Delete(string kind, string jti, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.KeyDeleteAsync($"{kind}:{jti}");
    }
}