using FluentAssertions;
using PolesTaxi.Shared.Errors;
using PolesTaxi.WalletService.App.Db.Pg;
using PolesTaxi.WalletService.Entity.Service;
using PolesTaxi.WalletService.Repository.Pg;
using Testcontainers.PostgreSql; // <- ИСПРАВЛЕНО: PostgreSql вместо PostgreSQL
using Xunit;

namespace PolesTaxi.WalletService.Tests;

public sealed class PostgresFixture : IAsyncLifetime
{
    // <- ИСПРАВЛЕНО: PostgreSqlContainer и PostgreSqlBuilder
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    public Task InitializeAsync() => Container.StartAsync();
    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

public sealed class WalletPgRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fx;
    public WalletPgRepositoryTests(PostgresFixture fx) => _fx = fx;

    private async Task<WalletPgRepository> Repo()
    {
        var pg = PgClient.New(_fx.Container.GetConnectionString());
        return new WalletPgRepository(pg.DataSource, null); // кэш Redis не нужен для тестов
    }

    [Fact]
    public async Task Create_and_get_wallet()
    {
        var repo = await Repo();
        var created = await repo.CreateWallet("u1", "RUB", CancellationToken.None);
        
        created.UserId.Should().Be("u1");
        created.Balance.Should().Be(0);
        created.Currency.Should().Be("RUB");
    }

    [Fact]
    public async Task Duplicate_user_is_conflict()
    {
        var repo = await Repo();
        await repo.CreateWallet("u2", "RUB", CancellationToken.None);
        
        var act = () => repo.CreateWallet("u2", "RUB", CancellationToken.None);
        
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.Code == ErrorCodes.Conflict);
    }

    [Fact]
    public async Task Atomic_credit_then_debit()
    {
        var repo = await Repo();
        await repo.CreateWallet("u3", "RUB", CancellationToken.None);
        
        await repo.ApplyMoney("u3", WalletTxType.Credit, 100, null, CancellationToken.None);
        await repo.ApplyMoney("u3", WalletTxType.Debit, 40, null, CancellationToken.None);
        
        var wallet = await repo.GetByUserId("u3", CancellationToken.None);
        wallet.Balance.Should().Be(60);
    }
}