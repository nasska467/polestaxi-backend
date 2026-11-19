using PolesTaxi.DriverService.Entity.Service;
using PolesTaxi.Shared.Types;

namespace PolesTaxi.DriverService.Repository;

public interface IDriverRepository
{
    Task<Driver> Insert(Driver driver, CancellationToken cancellationToken);
    Task<Driver> GetByUserId(string userId, CancellationToken cancellationToken);
    Task<Driver> SetStatus(string userId, DriverStatus status, CancellationToken cancellationToken);
    Task<IReadOnlyList<Driver>> FindAvailable(TaxiType taxiType, GeoPoint? near, int limit, CancellationToken cancellationToken);
}