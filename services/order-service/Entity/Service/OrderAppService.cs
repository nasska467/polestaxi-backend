using PolesTaxi.OrderService.Entity.Service;
using PolesTaxi.OrderService.Repository;
using PolesTaxi.Shared.Errors;
using PolesTaxi.Shared.Types;
using Location = PolesTaxi.OrderService.Entity.Service.Location;

namespace PolesTaxi.OrderService.Service;

public interface IOrderAppService
{
    Task<Order> CreateOrder(string userId, Location pickup, Location dropoff, TaxiType taxiType, CancellationToken cancellationToken);
    Task<Order> GetById(string id, CancellationToken cancellationToken);
    Task<Order> Cancel(string id, string userId, CancellationToken cancellationToken);
    Task<Order> RateOrder(string id, string userId, int score, CancellationToken cancellationToken); // <-- добавили
}

public sealed class OrderAppService : IOrderAppService
{
    private readonly IOrderRepository _orders;

    public OrderAppService(IOrderRepository orders) => _orders = orders;

    public async Task<Order> CreateOrder(string userId, Location pickup, Location dropoff, TaxiType taxiType, CancellationToken cancellationToken)
    {
        var distance = HaversineMeters(pickup, dropoff);
        var duration = (long)Math.Max(60, distance / 11.0);
        var km = distance / 1000.0;
        var multiplier = taxiType switch { TaxiType.Comfort => 1.4, TaxiType.Business => 2.0, _ => 1.0 };
        var price = Math.Round(100 + km * 15 * multiplier, 2);

        var order = new Order
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            TaxiType = taxiType,
            Status = OrderStatus.Created,
            Pickup = pickup,
            Dropoff = dropoff,
            DistanceMeters = (long)distance,
            DurationSeconds = duration,
            Price = price,
            Surge = 1,
            CreatedAt = DateTime.UtcNow
        };

        return await _orders.Index(order, cancellationToken);
    }

    public async Task<Order> GetById(string id, CancellationToken cancellationToken)
    {
        return await _orders.GetById(id, cancellationToken);
    }

    public async Task<Order> Cancel(string id, string userId, CancellationToken cancellationToken)
    {
        var order = await _orders.GetById(id, cancellationToken);
        if (order.UserId != userId) throw AppException.Forbidden("not your order");
        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            throw AppException.Conflict("cannot cancel order");

        var cancelled = new Order
        {
            Id = order.Id,
            UserId = order.UserId,
            DriverId = order.DriverId,
            TaxiType = order.TaxiType,
            Status = OrderStatus.Cancelled,
            Pickup = order.Pickup,
            Dropoff = order.Dropoff,
            DistanceMeters = order.DistanceMeters,
            DurationSeconds = order.DurationSeconds,
            Price = order.Price,
            Surge = order.Surge,
            CreatedAt = order.CreatedAt,
            AssignedAt = order.AssignedAt,
            StartedAt = order.StartedAt,
            FinishedAt = order.FinishedAt
        };

        return await _orders.Update(cancelled, cancellationToken);
    }

    // <-- добавили
    public async Task<Order> RateOrder(string id, string userId, int score, CancellationToken cancellationToken)
    {
        var order = await _orders.GetById(id, cancellationToken);
        if (order.UserId != userId) throw AppException.Forbidden("not your order");
        if (order.Status != OrderStatus.Completed)
            throw AppException.Conflict("only completed orders can be rated");
        if (score < 1 || score > 5)
            throw AppException.Validation("score must be between 1 and 5");

        var rated = new Order
        {
            Id = order.Id,
            UserId = order.UserId,
            DriverId = order.DriverId,
            TaxiType = order.TaxiType,
            Status = order.Status,
            Pickup = order.Pickup,
            Dropoff = order.Dropoff,
            DistanceMeters = order.DistanceMeters,
            DurationSeconds = order.DurationSeconds,
            Price = order.Price,
            Surge = order.Surge,
            UserRating = score,
            DriverRating = order.DriverRating,
            Comment = order.Comment,
            CreatedAt = order.CreatedAt,
            AssignedAt = order.AssignedAt,
            StartedAt = order.StartedAt,
            FinishedAt = order.FinishedAt
        };

        return await _orders.Update(rated, cancellationToken);
    }

    private static double HaversineMeters(Location a, Location b)
    {
        const double R = 6371000;
        var lat1 = a.Latitude * Math.PI / 180;
        var lat2 = b.Latitude * Math.PI / 180;
        var dLat = (b.Latitude - a.Latitude) * Math.PI / 180;
        var dLon = (b.Longitude - a.Longitude) * Math.PI / 180;

        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * R * Math.Asin(Math.Sqrt(h));
    }
}