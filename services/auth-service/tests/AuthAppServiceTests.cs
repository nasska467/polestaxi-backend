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

    private async Task<AuthAppService> Service()
    {
        var redis = await RedisClient.New(_fx.Container.GetConnectionString(), CancellationToken.None);
        var repo = new AuthRedisRepository(redis.Database);
        var gateway = new FakeUserGateway();
        
        // Ключ длиннее 32 байт (36 символов), чтобы HS256 работал
        return new AuthAppService(repo, gateway, "test-secret-key-for-jwt-tests-32bytes!!", TimeSpan.FromMinutes(5), TimeSpan.FromDays(1));
    }

    [Fact]
    public async Task Login_refresh_validate_logout()
    {
        var service = await Service();

        // 1. Login
        var pair = await service.Login("test@test.local", "password", CancellationToken.None);
        pair.AccessToken.Should().NotBeNullOrEmpty();
        pair.RefreshToken.Should().NotBeNullOrEmpty();

        // 2. Validate
        var principal = await service.ValidateToken(pair.AccessToken, CancellationToken.None);
        principal.UserId.Should().Be("u1");

        // 3. Refresh
        var newPair = await service.Refresh(pair.RefreshToken, CancellationToken.None);
        newPair.AccessToken.Should().NotBeNullOrEmpty();
        newPair.RefreshToken.Should().NotBeNullOrEmpty();

        // 4. Logout
        await service.Logout(newPair.AccessToken, newPair.RefreshToken, CancellationToken.None);

        // 5. После logout токен не должен проходить валидацию
        var act = () => service.ValidateToken(newPair.AccessToken, CancellationToken.None);
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.Code == ErrorCodes.Unauthorized);
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