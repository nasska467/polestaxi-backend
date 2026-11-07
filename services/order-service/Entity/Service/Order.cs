using PolesTaxi.Shared.Types;

namespace PolesTaxi.OrderService.Entity.Service;

public sealed class Location
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string? Address { get; init; }
}

public sealed class Order
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public string? DriverId { get; init; }
    public TaxiType TaxiType { get; init; }
    public OrderStatus Status { get; init; }
    public required Location Pickup { get; init; }
    public required Location Dropoff { get; init; }
    public long DistanceMeters { get; init; }
    public long DurationSeconds { get; init; }
    public double Price { get; init; }
    public double Surge { get; init; }
    public int? UserRating { get; init; }
    public int? DriverRating { get; init; }
    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? AssignedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? FinishedAt { get; init; }
}

public sealed class OrderSearch
{
    public OrderStatus? Status { get; init; }
    public string? UserId { get; init; }
    public string? DriverId { get; init; }
    public DateTime? CreatedFrom { get; init; }
    public DateTime? CreatedTo { get; init; }
    public int PageSize { get; init; } = 20;
}