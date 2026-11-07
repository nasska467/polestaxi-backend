using PolesTaxi.Shared.Errors;
using PolesTaxi.Shared.Types;
using PolesTaxi.UserService.App.Db.Mongo;
using PolesTaxi.UserService.Config;
using PolesTaxi.UserService.Entity.Service;
using PolesTaxi.UserService.Repository.mongo;

namespace PolesTaxi.UserService.App;

public static class Run
{
    public static async Task<int> ExecuteAsync(string[] args)
    {
        _ = args;
        var config = AppConfig.FromEnvironment();
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        MongoClient mongo;
        try
        {
            mongo = await MongoClient.New(config.MongoConnection, config.MongoDatabase, cts.Token);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"mongo init failed: {ex.Message}");
            return 1;
        }

        var users = new UserMongoRepository(mongo.Users);
        Console.WriteLine("mongodb ready");

        if (config.Seed)
        {
            await SeedAdmin(users, cts.Token);
        }

        try
        {
            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("shutdown");
        return 0;
    }

    private static async Task SeedAdmin(UserMongoRepository users, CancellationToken cancellationToken)
    {
        try
        {
            await users.GetByEmail("admin@polestaxi.local", cancellationToken);
            Console.WriteLine("seed: admin already exists");
        }
        catch (AppException ex) when (ex.Code == ErrorCodes.NotFound)
        {
            var now = DateTime.UtcNow;
            await users.Create(new User
            {
                Id = string.Empty,
                Name = "Admin",
                Phone = "+70000000000",
                Email = "admin@polestaxi.local",
                PasswordHash = "seed-not-a-real-hash",
                Roles = new[] { Role.Admin },
                Rating = 0,
                RecentRatings = Array.Empty<double>(),
                CreatedAt = now,
                UpdatedAt = now
            }, cancellationToken);
            Console.WriteLine("seed: admin created");
        }
    }
}