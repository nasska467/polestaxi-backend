using FluentAssertions;
using PolesTaxi.OrderService.App.Db.Elastic;
using PolesTaxi.OrderService.App.Db.Redis;
using PolesTaxi.OrderService.Entity.Service;
using PolesTaxi.OrderService.Repository.Elastic;
using PolesTaxi.OrderService.Repository.Redis;
using PolesTaxi.Shared.Types;
using Testcontainers.Elasticsearch;
using Testcontainers.Redis;
using Xunit;

namespace PolesTaxi.OrderService.Tests;

public sealed class OrderStoreFixture : IAsyncLifetime
{
    // Отключаем безопасность для тестов, чтобы не требовался пароль
    public ElasticsearchContainer Elastic { get; } = new ElasticsearchBuilder()
        .WithImage("docker.elastic.co/elasticsearch/elasticsearch:8.19.6")
        .WithEnvironment("xpack.security.enabled", "false")
        .WithEnvironment("xpack.security.http.ssl.enabled", "false")
        .Build();
        
    public RedisContainer Redis { get; } = new RedisBuilder().WithImage("redis:7.0").Build();

    public Task InitializeAsync() => Task.WhenAll(Elastic.StartAsync(), Redis.StartAsync());
    public Task DisposeAsync() => Task.WhenAll(Elastic.DisposeAsync().AsTask(), Redis.DisposeAsync().AsTask());
}

public sealed class OrderElasticRepositoryTests : IClassFixture<OrderStoreFixture>
{
    private readonly OrderStoreFixture _fx;
    private int _n;

    public OrderElasticRepositoryTests(OrderStoreFixture fx) => _fx = fx;

    private async Task<OrderElasticRepository> Repo()
    {
        _n++;
        var index = $"orders_{_n}_{Guid.NewGuid():N}";
        var elastic = await ElasticStore.New(_fx.Elastic.GetConnectionString(), index, CancellationToken.None);
        var redis = await RedisClient.New(_fx.Redis.GetConnectionString(), CancellationToken.None);
        var cache = new OrderRedisCache(redis.Database, TimeSpan.FromMinutes(5));
        return new OrderElasticRepository(elastic.Client, elastic.IndexName, cache);
    }

    private static Order NewOrder(string userId, OrderStatus status) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        UserId = userId,
        TaxiType = TaxiType.Comfort,
        Status = status,
        Pickup = new Location { Latitude = 55.75, Longitude = 37.62, Address = "A" },
        Dropoff = new Location { Latitude = 55.76, Longitude = 37.63, Address = "B" },
        DistanceMeters = 1000,
        DurationSeconds = 300,
        Price = 250,
        Surge = 1.1,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Index_and_get()
    {
        var repo = await Repo();
        var created = await repo.Index(NewOrder("u1", OrderStatus.Created), CancellationToken.None);
        var loaded = await repo.GetById(created.Id, CancellationToken.None);
        
        loaded.UserId.Should().Be("u1");
        loaded.Price.Should().Be(250);
    }

    [Fact]
    public async Task Search_by_status()
    {
        var repo = await Repo();
        await repo.Index(NewOrder("u2", OrderStatus.Created), CancellationToken.None);
        await repo.Index(NewOrder("u3", OrderStatus.Completed), CancellationToken.None);
        
        var found = await repo.Search(new OrderSearch { Status = OrderStatus.Completed }, CancellationToken.None);
        
        found.Should().Contain(o => o.UserId == "u3");
        found.Should().NotContain(o => o.UserId == "u2");
    }
}