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
}