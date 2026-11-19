using FluentAssertions;
using PolesTaxi.AnalyticService.App.Db.ClickHouse;
using PolesTaxi.AnalyticService.Entity.Service;
using PolesTaxi.AnalyticService.Repository.ClickHouse;
using PolesTaxi.Shared.Types;
using Testcontainers.ClickHouse;
using Xunit;

namespace PolesTaxi.AnalyticService.Tests;

public sealed class ClickHouseFixture : IAsyncLifetime
{
    public ClickHouseContainer Container { get; } = new ClickHouseBuilder()
        .WithImage("clickhouse/clickhouse-server:25.8")
        .Build();

    public Task InitializeAsync() => Container.StartAsync();
    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

public sealed class AnalyticRepositoryTests : IClassFixture<ClickHouseFixture>
{
    private readonly ClickHouseFixture _fx;
    public AnalyticRepositoryTests(ClickHouseFixture fx) => _fx = fx;

    private async Task<AnalyticClickHouseRepository> Repo()
    {
        var connectionString = _fx.Container.GetConnectionString();
        await ClickHouseStore.New(connectionString, CancellationToken.None);
        return new AnalyticClickHouseRepository(connectionString);
    }

    [Fact]
    public async Task Insert_and_aggregate()
    {
        var repo = await Repo();
        var now = DateTime.UtcNow;

        await repo.InsertOrderCompleted(new OrderCompletedFact
        {
            OrderId = "o1",
            UserId = "u1",
            DriverId = "d1",
            TaxiType = TaxiType.Economy,
            Price = 100,
            DistanceMeters = 2000,
            EventTime = now
        }, CancellationToken.None);

        await repo.InsertTripRated(new TripRatedFact
        {
            OrderId = "o1",
            FromRole = Role.User,
            Score = 5,
            EventTime = now
        }, CancellationToken.None);

        await Task.Delay(500);

        var orderStats = await repo.GetOrderStats(now.AddMinutes(-1), now.AddMinutes(1), CancellationToken.None);
        orderStats.Total.Should().BeGreaterOrEqualTo(1);
        orderStats.AvgPrice.Should().Be(100);

        var ratingStats = await repo.GetRatingStats(now.AddMinutes(-1), now.AddMinutes(1), CancellationToken.None);
        ratingStats.Total.Should().BeGreaterOrEqualTo(1);
        ratingStats.AvgUserRating.Should().Be(5);
    }
}