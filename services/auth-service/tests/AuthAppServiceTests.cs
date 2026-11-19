using FluentAssertions;
using PolesTaxi.AuthService.App.Db.Redis;
using PolesTaxi.AuthService.Entity.Service;
using PolesTaxi.AuthService.Gateway;
using PolesTaxi.AuthService.Repository.Redis;
using PolesTaxi.AuthService.Service;
using PolesTaxi.Shared.Errors;
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

public sealed class AuthAppServiceTests : IClassFixture<RedisFixture>
{
    private readonly RedisFixture _fx;
    public AuthAppServiceTests(RedisFixture fx) => _fx = fx;

    private async Task<AuthRedisRepository> Repo()
    {
        var redis = await RedisClient.New(_fx.Container.GetConnectionString(), CancellationToken.None);
        return new AuthRedisRepository(redis.Database);
    }

    private async Task<AuthAppService> Service()
    {
        var repo = await Repo();
        var gateway = new FakeUserGateway();
        return new AuthAppService(repo, gateway, "test-secret-key-for-jwt-tests-32bytes!!", TimeSpan.FromMinutes(5), TimeSpan.FromDays(1));
    }

    [Fact]
    public async Task Login_refresh_validate_logout()
    {
        var service = await Service();

        var pair = await service.Login("test@test.local", "password", CancellationToken.None);
        pair.AccessToken.Should().NotBeNullOrEmpty();
        pair.RefreshToken.Should().NotBeNullOrEmpty();

        var principal = await service.ValidateToken(pair.AccessToken, CancellationToken.None);
        principal.UserId.Should().Be("u1");

        var newPair = await service.Refresh(pair.RefreshToken, CancellationToken.None);
        newPair.AccessToken.Should().NotBeNullOrEmpty();
        newPair.RefreshToken.Should().NotBeNullOrEmpty();

        await service.Logout(newPair.AccessToken, newPair.RefreshToken, CancellationToken.None);

        var act = () => service.ValidateToken(newPair.AccessToken, CancellationToken.None);
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.Code == ErrorCodes.Unauthorized);
    }

    [Fact]
    public async Task Set_and_get_access()
    {
        var repo = await Repo();
        var token = new TokenRecord { Jti = "a1", UserId = "u1", Roles = new[] { Role.User }, Family = "fam" };
        await repo.SetAccess(token, TimeSpan.FromMinutes(5), CancellationToken.None);

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
        await repo.SetAccess(new TokenRecord { Jti = "acc", UserId = "u1", Roles = new[] { Role.User } }, TimeSpan.FromMinutes(5), CancellationToken.None);
        await repo.SetRefresh(new TokenRecord { Jti = "ref", UserId = "u1", Roles = new[] { Role.User } }, TimeSpan.FromMinutes(5), CancellationToken.None);

        await repo.DeletePair("acc", "ref", CancellationToken.None);

        (await repo.GetAccess("acc", CancellationToken.None)).Should().BeNull();
        (await repo.GetRefresh("ref", CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Expired_access_is_gone()
    {
        var repo = await Repo();
        await repo.SetAccess(new TokenRecord { Jti = "ttl", UserId = "u1", Roles = new[] { Role.User } }, TimeSpan.FromMilliseconds(200), CancellationToken.None);
        await Task.Delay(600);

        (await repo.GetAccess("ttl", CancellationToken.None)).Should().BeNull();
    }

    private sealed class FakeUserGateway : IUserGateway
    {
        public Task<UserPrincipal> VerifyCredentials(string login, string password, CancellationToken cancellationToken)
        {
            return Task.FromResult(new UserPrincipal
            {
                UserId = "u1",
                Roles = new[] { Role.User }
            });
        }
    }
}