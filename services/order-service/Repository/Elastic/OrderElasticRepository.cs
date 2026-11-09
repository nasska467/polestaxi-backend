using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using PolesTaxi.OrderService.Entity.Repository;
using PolesTaxi.OrderService.Entity.Service;
using PolesTaxi.OrderService.Repository.Redis;
using PolesTaxi.Shared.Errors;
using PolesTaxi.Shared.Types;
using Location = PolesTaxi.OrderService.Entity.Service.Location;

namespace PolesTaxi.OrderService.Repository.Elastic;

public sealed class OrderElasticRepository : IOrderRepository
{
    private readonly ElasticsearchClient _client;
    private readonly string _index;
    private readonly OrderRedisCache? _cache;

    public OrderElasticRepository(ElasticsearchClient client, string index, OrderRedisCache? cache = null)
    {
        _client = client;
        _index = index;
        _cache = cache;
    }

    public async Task<Order> Index(Order order, CancellationToken cancellationToken)
    {
        var doc = ToDocument(order);
        if (string.IsNullOrEmpty(doc.Id)) doc.Id = Guid.NewGuid().ToString("N");

        var response = await _client.IndexAsync(doc, i => i.Index(_index).Id(doc.Id).Refresh(Refresh.True), cancellationToken);
        if (!response.IsValidResponse) throw AppException.Internal(response.DebugInformation);

        var domain = ToDomain(doc);
        if (_cache is not null) await _cache.Set(domain, cancellationToken);
        return domain;
    }

    public async Task<Order> GetById(string id, CancellationToken cancellationToken)
    {
        if (_cache is not null)
        {
            var cached = await _cache.Get(id, cancellationToken);
            if (cached is not null) return cached;
        }

        var response = await _client.GetAsync<OrderDocument>(id, g => g.Index(_index), cancellationToken);
        if (!response.Found || response.Source is null) throw AppException.NotFound("order not found");

        var domain = ToDomain(response.Source);
        if (_cache is not null) await _cache.Set(domain, cancellationToken);
        return domain;
    }

    public async Task<IReadOnlyList<Order>> Search(OrderSearch query, CancellationToken cancellationToken)
    {
        var filters = new List<Query>();
        if (query.Status is { } status && status != OrderStatus.Unspecified)
            filters.Add(Query.Term(new TermQuery("status"!) { Value = StatusToWire(status) }));

        if (!string.IsNullOrEmpty(query.UserId))
            filters.Add(Query.Term(new TermQuery("userId"!) { Value = query.UserId }));

        if (!string.IsNullOrEmpty(query.DriverId))
            filters.Add(Query.Term(new TermQuery("driverId"!) { Value = query.DriverId }));

        var response = await _client.SearchAsync<OrderDocument>(s => s
            .Index(_index)
            .Query(filters.Count == 0 ? Query.MatchAll(new MatchAllQuery()) : Query.Bool(new BoolQuery { Filter = filters }))
            .Size(query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100)), cancellationToken);

        if (!response.IsValidResponse) throw AppException.Internal(response.DebugInformation);
        return response.Documents.Select(ToDomain).ToList();
    }

    public async Task<Order> Update(Order order, CancellationToken cancellationToken)
    {
        var doc = ToDocument(order);
        var response = await _client.IndexAsync(doc, i => i.Index(_index).Id(doc.Id).Refresh(Refresh.True), cancellationToken);
        if (!response.IsValidResponse) throw AppException.Internal(response.DebugInformation);

        var domain = ToDomain(doc);
        if (_cache is not null) await _cache.Set(domain, cancellationToken);
        return domain;
    }

    private static OrderDocument ToDocument(Order order) => new()
    {
        Id = order.Id,
        UserId = order.UserId,
        DriverId = order.DriverId,
        TaxiType = order.TaxiType.ToWire(),
        Status = StatusToWire(order.Status),
        Pickup = new OrderLocationDocument { Lat = order.Pickup.Latitude, Lon = order.Pickup.Longitude, Address = order.Pickup.Address },
        Dropoff = new OrderLocationDocument { Lat = order.Dropoff.Latitude, Lon = order.Dropoff.Longitude, Address = order.Dropoff.Address },
        DistanceMeters = order.DistanceMeters,
        DurationSeconds = order.DurationSeconds,
        Price = order.Price,
        Surge = order.Surge,
        UserRating = order.UserRating,
        DriverRating = order.DriverRating,
        Comment = order.Comment,
        CreatedAt = order.CreatedAt,
        AssignedAt = order.AssignedAt,
        StartedAt = order.StartedAt,
        FinishedAt = order.FinishedAt
    };

    private static Order ToDomain(OrderDocument doc) => new()
    {
        Id = doc.Id,
        UserId = doc.UserId,
        DriverId = doc.DriverId,
        TaxiType = EnumMaps.TaxiTypeFromWire(doc.TaxiType),
        Status = StatusFromWire(doc.Status),
        Pickup = new Location { Latitude = doc.Pickup.Lat, Longitude = doc.Pickup.Lon, Address = doc.Pickup.Address },
        Dropoff = new Location { Latitude = doc.Dropoff.Lat, Longitude = doc.Dropoff.Lon, Address = doc.Dropoff.Address },
        DistanceMeters = doc.DistanceMeters,
        DurationSeconds = doc.DurationSeconds,
        Price = doc.Price,
        Surge = doc.Surge,
        UserRating = doc.UserRating,
        DriverRating = doc.DriverRating,
        Comment = doc.Comment,
        CreatedAt = doc.CreatedAt,
        AssignedAt = doc.AssignedAt,
        StartedAt = doc.StartedAt,
        FinishedAt = doc.FinishedAt
    };

    private static string StatusToWire(OrderStatus status) => status switch
    {
        OrderStatus.Created => "created",
        OrderStatus.DriverAssigned => "driver_assigned",
        OrderStatus.InProgress => "in_progress",
        OrderStatus.Completed => "completed",
        OrderStatus.Cancelled => "cancelled",
        _ => "unspecified"
    };

    private static OrderStatus StatusFromWire(string value) => value switch
    {
        "created" => OrderStatus.Created,
        "driver_assigned" => OrderStatus.DriverAssigned,
        "in_progress" => OrderStatus.InProgress,
        "completed" => OrderStatus.Completed,
        "cancelled" => OrderStatus.Cancelled,
        _ => OrderStatus.Unspecified
    };
}