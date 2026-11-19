using StackExchange.Redis;

namespace PolesTaxi.WalletService.App.Db.Redis;

public sealed class RedisClient : IDisposable
{
    public ConnectionMultiplexer Multiplexer { get; }
    public IDatabase Database { get; }

    private RedisClient(ConnectionMultiplexer multiplexer)
    {
        Multiplexer = multiplexer;
        Database = multiplexer.GetDatabase();
    }

    public static async Task<RedisClient> New(string connectionString, CancellationToken cancellationToken)
    {
        var mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        cancellationToken.ThrowIfCancellationRequested();
        return new RedisClient(mux);
    }

    public void Dispose() => Multiplexer.Dispose();
}