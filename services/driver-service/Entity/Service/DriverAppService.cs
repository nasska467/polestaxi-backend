using PolesTaxi.DriverService.Entity.Service;
using PolesTaxi.DriverService.Repository;
using PolesTaxi.Shared.Errors;
using PolesTaxi.Shared.Types;

namespace PolesTaxi.DriverService.Service;

public interface IDriverAppService
{
    Task<Driver> Register(string userId, CancellationToken cancellationToken);
    Task<Driver> GetByUserId(string userId, CancellationToken cancellationToken);
    Task<Driver> SetStatus(string userId, DriverStatus status, CancellationToken cancellationToken);
    Task<IReadOnlyList<Driver>> FindAvailable(TaxiType taxiType, double lat, double lon, int limit, CancellationToken cancellationToken);
    
    // ДОБАВЛЕНО: Методы для поездки
    Task AcceptTrip(string userId, string orderId, CancellationToken cancellationToken);
    Task DeclineTrip(string userId, string orderId, CancellationToken cancellationToken);
    Task StartTrip(string userId, string orderId, CancellationToken cancellationToken);
    Task CompleteTrip(string userId, string orderId, CancellationToken cancellationToken);
}

public sealed class DriverAppService : IDriverAppService
{
    private readonly IDriverRepository _drivers;

    public DriverAppService(IDriverRepository drivers) => _drivers = drivers;

    public async Task<Driver> Register(string userId, CancellationToken cancellationToken)
    {
        var driver = new Driver
        {
            Id = string.Empty,
            UserId = userId,
            TaxiType = TaxiType.Economy,
            LicensePlate = "PENDING",
            Status = DriverStatus.Offline,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        return await _drivers.Insert(driver, cancellationToken);
    }

    public async Task<Driver> GetByUserId(string userId, CancellationToken cancellationToken)
    {
        return await _drivers.GetByUserId(userId, cancellationToken);
    }

    public async Task<Driver> SetStatus(string userId, DriverStatus status, CancellationToken cancellationToken)
    {
        var driver = await _drivers.GetByUserId(userId, cancellationToken);
        if (!EnumMaps.CanTransition(driver.Status, status))
            throw AppException.Conflict("cannot transition driver status");

        return await _drivers.SetStatus(userId, status, cancellationToken);
    }

    public async Task<IReadOnlyList<Driver>> FindAvailable(TaxiType taxiType, double lat, double lon, int limit, CancellationToken cancellationToken)
    {
        GeoPoint? point = lat != 0 || lon != 0 ? new GeoPoint { Latitude = lat, Longitude = lon } : null;
        return await _drivers.FindAvailable(taxiType, point, limit, cancellationToken);
    }

    // ДОБАВЛЕНО: Водитель принимает заказ (Available -> OnTrip)
    public async Task AcceptTrip(string userId, string orderId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            throw AppException.Validation("order_id is required");
        
        await SetTripStatus(userId, DriverStatus.Available, DriverStatus.OnTrip, cancellationToken);
    }

    // ДОБАВЛЕНО: Водитель отказывается от заказа (OnTrip -> Available)
    public async Task DeclineTrip(string userId, string orderId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            throw AppException.Validation("order_id is required");
        
        await Release(userId, cancellationToken);
    }

    // ДОБАВЛЕНО: Водитель начинает поездку (Available -> OnTrip, если еще не в поездке)
    public async Task StartTrip(string userId, string orderId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            throw AppException.Validation("order_id is required");
        
        await EnsureOnTrip(userId, cancellationToken);
    }

    // ДОБАВЛЕНО: Водитель завершает поездку (OnTrip -> Available)
    public async Task CompleteTrip(string userId, string orderId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            throw AppException.Validation("order_id is required");
        
        await SetTripStatus(userId, DriverStatus.OnTrip, DriverStatus.Available, cancellationToken);
    }

    // Вспомогательный метод для смены статуса
    private async Task SetTripStatus(string userId, DriverStatus from, DriverStatus to, CancellationToken cancellationToken)
    {
        var driver = await _drivers.GetByUserId(userId, cancellationToken);
        if (driver.Status != from && driver.Status != to)
            throw AppException.Conflict("cannot apply trip action");

        await _drivers.SetStatus(userId, to, cancellationToken);
    }

    // Вспомогательный метод: убедиться, что водитель в поездке
    private async Task EnsureOnTrip(string userId, CancellationToken cancellationToken)
    {
        var driver = await _drivers.GetByUserId(userId, cancellationToken);
        if (driver.Status == DriverStatus.OnTrip) return; // Уже в поездке
        if (driver.Status != DriverStatus.Available)
            throw AppException.Conflict("cannot apply trip action");

        await _drivers.SetStatus(userId, DriverStatus.OnTrip, cancellationToken);
    }

    // Вспомогательный метод: освободить водителя
    private async Task Release(string userId, CancellationToken cancellationToken)
    {
        var driver = await _drivers.GetByUserId(userId, cancellationToken);
        if (driver.Status == DriverStatus.OnTrip)
        {
            await _drivers.SetStatus(userId, DriverStatus.Available, cancellationToken);
        }
        else if (driver.Status == DriverStatus.Offline)
        {
            throw AppException.Conflict("cannot apply trip action");
        }
        // Если Available — ничего не меняем
    }
}