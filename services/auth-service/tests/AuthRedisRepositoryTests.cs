using FluentAssertions;
using PolesTaxi.AuthService.App.Db.Redis;
using PolesTaxi.AuthService.Entity.Service;
using PolesTaxi.AuthService.Repository.Redis;
using PolesTaxi.Shared.Types;
using Testcontainers.Redis;
using Xunit;

namespace PolesTaxi.AuthService.Tests;

public sealed class RedisFixture : IAsyncLifetime
{
    public RedisContainer Container { get; } = new RedisBuilder().WithImage("redis:7.0").Build();
    public Task InitializeAsync() => Container.StartAsync();
    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

public sealed class AuthRedisRepositoryTests : IClassFixture<RedisFixture>
{
    private readonly RedisFixture _fx;
    public AuthRedisRepositoryTests(RedisFixture fx) => _fx = fx;

    private async Task<AuthRedisRepository> Repo()
    {
        var client = await RedisClient.New(_fx.Container.GetConnectionString(), CancellationToken.None);
        return new AuthRedisRepository(client.Database);
    }

    private static TokenRecord Tok(string jti) => new()
    {
        Jti = jti,
        UserId = "u1",
        Roles = new[] { Role.User },
        Family = "fam"
    };

    [Fact]
    public async Task Set_and_get_access()
    {
        var repo = await Repo();
        await repo.SetAccess(Tok("a1"), TimeSpan.FromMinutes(5), CancellationToken.None);
        var got = await repo.GetAccess("a1", CancellationToken.None);
        
        got.Should().NotBeNull();
        got!.UserId.Should().Be("u1");
        got.Roles.Should().Contain(Role.User);
    }

    [Fact]
    public async Task Missing_key_is_null()
    {
        var repo = await Repo();
        (await repo.GetRefresh("nope", CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Delete_pair_removes_both()
    {
        var repo = await Repo();
        await repo.SetAccess(Tok("acc"), TimeSpan.FromMinutes(5), CancellationToken.None);
        await repo.SetRefresh(Tok("ref"), TimeSpan.FromMinutes(5), CancellationToken.None);
        
        await repo.DeletePair("acc", "ref", CancellationToken.None);
        
        (await repo.GetAccess("acc", CancellationToken.None)).Should().BeNull();
        (await repo.GetRefresh("ref", CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Expired_access_is_gone()
    {
        var repo = await Repo();
        await repo.SetAccess(Tok("ttl"), TimeSpan.FromMilliseconds(200), CancellationToken.None);
        await Task.Delay(600);
        
        (await repo.GetAccess("ttl", CancellationToken.None)).Should().BeNull();
    }
}