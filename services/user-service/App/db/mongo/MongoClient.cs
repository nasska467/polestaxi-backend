using MongoDB.Driver;
using PolesTaxi.UserService.Entity.Repository;

namespace PolesTaxi.UserService.App.Db.Mongo;

public sealed class MongoClient
{
    public IMongoDatabase Database { get; }
    public IMongoCollection<UserDocument> Users { get; }

    private MongoClient(IMongoDatabase database)
    {
        Database = database;
        Users = database.GetCollection<UserDocument>("users");
    }

    public static async Task<MongoClient> New(string connectionString, string databaseName, CancellationToken cancellationToken)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        
        // Обращаемся к драйверу через полное имя, чтобы не было конфликта с нашим классом
        var inner = new MongoDB.Driver.MongoClient(settings); 
        
        var db = inner.GetDatabase(databaseName);
        var client = new MongoClient(db);
        await client.EnsureIndexes(cancellationToken);
        return client;
    }

    private async Task EnsureIndexes(CancellationToken cancellationToken)
    {
        var email = new CreateIndexModel<UserDocument>(
            Builders<UserDocument>.IndexKeys.Ascending(x => x.Email),
            new CreateIndexOptions { Unique = true, Name = "ux_users_email" });
            
        var phone = new CreateIndexModel<UserDocument>(
            Builders<UserDocument>.IndexKeys.Ascending(x => x.Phone),
            new CreateIndexOptions { Unique = true, Name = "ux_users_phone" });
            
        var deleted = new CreateIndexModel<UserDocument>(
            Builders<UserDocument>.IndexKeys.Ascending(x => x.IsDeleted),
            new CreateIndexOptions { Name = "ix_users_isDeleted" });

        await Users.Indexes.CreateManyAsync(new[] { email, phone, deleted }, cancellationToken);
    }
}