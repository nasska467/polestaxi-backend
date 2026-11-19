using PolesTaxi.OrderService.Config;
using PolesTaxi.OrderService.Entity.Service;
using PolesTaxi.OrderService.Gateway;
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
    Task<Order> RateOrder(string id, string userId, int score, CancellationToken cancellationToken);
    
    // ДОБАВЛЕНО: Методы для смены статуса
    Task<Order> Accept(string orderId, string driverId, CancellationToken cancellationToken);
    Task<Order> Start(string orderId, string driverId, CancellationToken cancellationToken);
    Task<Order> Complete(string orderId, string driverId, CancellationToken cancellationToken);
}

public sealed class OrderAppService : IOrderAppService
{
    private readonly IOrderRepository _orders;
    private readonly IWalletGateway _wallets;
    private readonly IDriverGateway _drivers; // ДОБАВЛЕНО
    private readonly PricingConfig _pricing;

    public OrderAppService(IOrderRepository orders, IWalletGateway wallets, IDriverGateway drivers, PricingConfig pricing)
    {
        _orders = orders;
        _wallets = wallets;
        _drivers = drivers; // ДОБАВЛЕНО
        _pricing = pricing;
    }

    public async Task<Order> CreateOrder(string userId, Location pickup, Location dropoff, TaxiType taxiType, CancellationToken cancellationToken)
    {
        var distance = HaversineMeters(pickup, dropoff);
        var duration = (long)Math.Max(60, distance / 11.0);

        var price = (double)Quote((long)distance, duration, taxiType);

        var id = Guid.NewGuid().ToString("N");

        if (_wallets is not null)
        {
            await _wallets.Debit(userId, id, (decimal)price, cancellationToken);
        }

        try
        {
            var order = new Order
            {
                Id = id,
                UserId = userId,
                TaxiType = taxiType,
                Status = OrderStatus.Created,
                Pickup = pickup,
                Dropoff = dropoff,
                DistanceMeters = (long)distance,
                DurationSeconds = duration,
                Price = price,
                Surge = (double)_pricing.Surge,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _orders.Index(order, cancellationToken);
            return created;
        }
        catch
        {
            if (_wallets is not null)
            {
                try
                {
                    await _wallets.Refund(userId, id, (decimal)price, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"refund after create failed: {ex.Message}");
                }
            }
            throw;
        }
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

        if (_wallets is not null && order.Status != OrderStatus.Cancelled)
        {
            await _wallets.Refund(order.UserId, order.Id, (decimal)order.Price, cancellationToken);
        }

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

    // ДОБАВЛЕНО: Водитель принимает заказ
    public async Task<Order> Accept(string orderId, string driverId, CancellationToken cancellationToken)
    {
        var order = await _orders.GetById(orderId, cancellationToken);
        if (order.Status != OrderStatus.Created)
            throw AppException.Conflict("order is not available for accept");

        if (_drivers is not null)
        {
            await _drivers.AcceptTrip(driverId, orderId, cancellationToken);
        }

        var assigned = new Order
        {
            Id = order.Id,
            UserId = order.UserId,
            DriverId = driverId,
            TaxiType = order.TaxiType,
            Status = OrderStatus.DriverAssigned,
            Pickup = order.Pickup,
            Dropoff = order.Dropoff,
            DistanceMeters = order.DistanceMeters,
            DurationSeconds = order.DurationSeconds,
            Price = order.Price,
            Surge = order.Surge,
            CreatedAt = order.CreatedAt,
            AssignedAt = DateTime.UtcNow
        };

        return await _orders.Update(assigned, cancellationToken);
    }

    // ДОБАВЛЕНО: Водитель начинает поездку
    public async Task<Order> Start(string orderId, string driverId, CancellationToken cancellationToken)
    {
        var order = await _orders.GetById(orderId, cancellationToken);
        if (order.Status != OrderStatus.DriverAssigned)
            throw AppException.Conflict("order is not assigned");

        if (_drivers is not null)
        {
            await _drivers.StartTrip(driverId, orderId, cancellationToken);
        }

        var started = new Order
        {
            Id = order.Id,
            UserId = order.UserId,
            DriverId = order.DriverId,
            TaxiType = order.TaxiType,
            Status = OrderStatus.InProgress,
            Pickup = order.Pickup,
            Dropoff = order.Dropoff,
            DistanceMeters = order.DistanceMeters,
            DurationSeconds = order.DurationSeconds,
            Price = order.Price,
            Surge = order.Surge,
            CreatedAt = order.CreatedAt,
            AssignedAt = order.AssignedAt,
            StartedAt = DateTime.UtcNow
        };

        return await _orders.Update(started, cancellationToken);
    }

    // ДОБАВЛЕНО: Водитель завершает поездку
    public async Task<Order> Complete(string orderId, string driverId, CancellationToken cancellationToken)
    {
        var order = await _orders.GetById(orderId, cancellationToken);
        if (order.Status != OrderStatus.InProgress)
            throw AppException.Conflict("order is not in progress");

        if (_drivers is not null)
        {
            await _drivers.CompleteTrip(driverId, orderId, cancellationToken);
        }

        var completed = new Order
        {
            Id = order.Id,
            UserId = order.UserId,
            DriverId = order.DriverId,
            TaxiType = order.TaxiType,
            Status = OrderStatus.Completed,
            Pickup = order.Pickup,
            Dropoff = order.Dropoff,
            DistanceMeters = order.DistanceMeters,
            DurationSeconds = order.DurationSeconds,
            Price = order.Price,
            Surge = order.Surge,
            CreatedAt = order.CreatedAt,
            AssignedAt = order.AssignedAt,
            StartedAt = order.StartedAt,
            FinishedAt = DateTime.UtcNow
        };

        return await _orders.Update(completed, cancellationToken);
    }

    private decimal Quote(long distanceMeters, long durationSeconds, TaxiType taxiType)
    {
        var km = distanceMeters / 1000m;
        var minutes = durationSeconds / 60m;
        
        var multiplier = taxiType switch
        {
            TaxiType.Comfort => _pricing.ComfortMultiplier,
            TaxiType.Business => _pricing.BusinessMultiplier,
            _ => 1m
        };

        var raw = (_pricing.Base + km * _pricing.PerKm + minutes * _pricing.PerMin) * multiplier * _pricing.Surge;
        return Math.Round(raw, 2);
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