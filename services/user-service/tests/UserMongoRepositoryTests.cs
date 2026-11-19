using FluentAssertions;
using PolesTaxi.Shared.Errors;
using PolesTaxi.Shared.Types;
using PolesTaxi.UserService.App.Db.Mongo;
using PolesTaxi.UserService.Entity.Service;
using PolesTaxi.UserService.Repository.mongo;
using Testcontainers.MongoDb;
using Xunit;

namespace PolesTaxi.UserService.Tests;

public sealed class MongoFixture : IAsyncLifetime
{
    public MongoDbContainer Container { get; } = new MongoDbBuilder().WithImage("mongo:7").Build();
    public Task InitializeAsync() => Container.StartAsync();
    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

public sealed class UserMongoRepositoryTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _fx;

    public UserMongoRepositoryTests(MongoFixture fx) => _fx = fx;

    private async Task<UserMongoRepository> Repo()
    {
        // Каждый тест создает уникальную базу, чтобы изолировать данные
        var dbName = $"users_{Guid.NewGuid():N}";
        var mongo = await MongoClient.New(_fx.Container.GetConnectionString(), dbName, CancellationToken.None);
        return new UserMongoRepository(mongo.Users);
    }

    private static User NewUser(string email) => new()
    {
        Id = string.Empty,
        Name = "Test",
        Phone = "+79990000000",
        Email = email,
        PasswordHash = "hash",
        Roles = new[] { Role.User },
        Rating = 0,
        RecentRatings = Array.Empty<double>(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Create_and_get_by_email()
    {
        var repo = await Repo();
        var created = await repo.Create(NewUser("test@test.local"), CancellationToken.None);
        var loaded = await repo.GetByEmail("test@test.local", CancellationToken.None);
        
        loaded.Id.Should().Be(created.Id);
        loaded.Name.Should().Be("Test");
    }

    [Fact]
    public async Task Duplicate_email_is_conflict()
    {
        var repo = await Repo();
        await repo.Create(NewUser("dup@test.local"), CancellationToken.None);
        
        var act = () => repo.Create(NewUser("dup@test.local"), CancellationToken.None);
        
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.Code == ErrorCodes.Conflict);
    }

    [Fact]
    public async Task Soft_delete_hides_user()
    {
        var repo = await Repo();
        var created = await repo.Create(NewUser("delete@test.local"), CancellationToken.None);
        
        await repo.SoftDelete(created.Id, CancellationToken.None);
        
        var act = () => repo.GetById(created.Id, CancellationToken.None);
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.Code == ErrorCodes.NotFound);
    }
}