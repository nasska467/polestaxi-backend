using PolesTaxi.Shared.Types;

namespace PolesTaxi.DriverService.Entity.Service;

public sealed class GeoPoint
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}

public sealed class Driver
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public TaxiType TaxiType { get; init; }
    public required string LicensePlate { get; init; }
    public DriverStatus Status { get; init; }
    public GeoPoint? Location { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}