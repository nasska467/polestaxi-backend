using MongoDB.Driver;
using PolesTaxi.DriverService.Entity.Repository;

namespace PolesTaxi.DriverService.App.Db.Mongo;

public sealed class MongoClient
{
    public IMongoDatabase Database { get; }
    public IMongoCollection<DriverDocument> Drivers { get; }

    private MongoClient(IMongoDatabase database)
    {
        Database = database;
        Drivers = database.GetCollection<DriverDocument>("drivers");
    }

    public static async Task<MongoClient> New(string connectionString, string databaseName, CancellationToken cancellationToken)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        var inner = new MongoDB.Driver.MongoClient(settings);
        var client = new MongoClient(inner.GetDatabase(databaseName));
        await client.EnsureIndexes(cancellationToken);
        return client;
    }

    private async Task EnsureIndexes(CancellationToken cancellationToken)
    {
        var userId = new CreateIndexModel<DriverDocument>(
            Builders<DriverDocument>.IndexKeys.Ascending(x => x.UserId),
            new CreateIndexOptions { Unique = true, Name = "ux_drivers_userId" });

        var statusType = new CreateIndexModel<DriverDocument>(
            Builders<DriverDocument>.IndexKeys.Ascending(x => x.Status).Ascending(x => x.TaxiType),
            new CreateIndexOptions { Name = "ix_drivers_status_taxiType" });

        var geo = new CreateIndexModel<DriverDocument>(
            Builders<DriverDocument>.IndexKeys.Geo2DSphere(x => x.Location),
            new CreateIndexOptions { Name = "ix_drivers_location" });

        await Drivers.Indexes.CreateManyAsync(new[] { userId, statusType, geo }, cancellationToken);
    }
}