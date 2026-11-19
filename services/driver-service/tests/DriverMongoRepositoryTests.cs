using FluentAssertions;
using PolesTaxi.DriverService.App.Db.Mongo;
using PolesTaxi.DriverService.Entity.Service;
using PolesTaxi.DriverService.Repository.Mongo;
using PolesTaxi.Shared.Errors;
using PolesTaxi.Shared.Types;
using Testcontainers.MongoDb;
using Xunit;

namespace PolesTaxi.DriverService.Tests;

public sealed class MongoFixture : IAsyncLifetime
{
    public MongoDbContainer Container { get; } = new MongoDbBuilder().WithImage("mongo:7").Build();
    public Task InitializeAsync() => Container.StartAsync();
    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

public sealed class DriverMongoRepositoryTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _fx;

    public DriverMongoRepositoryTests(MongoFixture fx) => _fx = fx;

    private async Task<DriverMongoRepository> Repo()
    {
        var dbName = $"drivers_{Guid.NewGuid():N}";
        var mongo = await MongoClient.New(_fx.Container.GetConnectionString(), dbName, CancellationToken.None);
        return new DriverMongoRepository(mongo.Drivers);
    }

    private static Driver NewDriver(string userId, DriverStatus status) => new()
    {
        Id = string.Empty,
        UserId = userId,
        TaxiType = TaxiType.Economy,
        LicensePlate = "A111AA77",
        Status = status,
        Location = new GeoPoint { Latitude = 55.75, Longitude = 37.62 },
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Insert_and_get_by_user_id()
    {
        var repo = await Repo();
        var created = await repo.Insert(NewDriver("u1", DriverStatus.Offline), CancellationToken.None);
        var loaded = await repo.GetByUserId("u1", CancellationToken.None);
        
        loaded.Id.Should().Be(created.Id);
        loaded.LicensePlate.Should().Be("A111AA77");
    }

    [Fact]
    public async Task Duplicate_user_id_is_conflict()
    {
        var repo = await Repo();
        await repo.Insert(NewDriver("u2", DriverStatus.Offline), CancellationToken.None);
        
        var act = () => repo.Insert(NewDriver("u2", DriverStatus.Available), CancellationToken.None);
        
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.Code == ErrorCodes.Conflict);
    }

    [Fact]
    public async Task Find_available_by_status()
    {
        var repo = await Repo();
        await repo.Insert(NewDriver("u3", DriverStatus.Available), CancellationToken.None);
        await repo.Insert(NewDriver("u4", DriverStatus.Offline), CancellationToken.None);
        
        var found = await repo.FindAvailable(TaxiType.Economy, null, 10, CancellationToken.None);
        
        found.Should().ContainSingle(d => d.UserId == "u3");
        found.Should().NotContain(d => d.UserId == "u4");
    }
}