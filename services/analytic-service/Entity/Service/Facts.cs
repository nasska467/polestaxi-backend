using PolesTaxi.Shared.Types;

namespace PolesTaxi.AnalyticService.Entity.Service;

public sealed class UserRegisteredFact
{
    public required string UserId { get; init; }
    public Role Role { get; init; }
    public DateTime EventTime { get; init; }
}

public sealed class OrderCompletedFact
{
    public required string OrderId { get; init; }
    public required string UserId { get; init; }
    public required string DriverId { get; init; }
    public TaxiType TaxiType { get; init; }
    public decimal Price { get; init; }
    public long DistanceMeters { get; init; }
    public DateTime EventTime { get; init; }
}

public sealed class TripRatedFact
{
    public required string OrderId { get; init; }
    public Role FromRole { get; init; }
    public int Score { get; init; }
    public DateTime EventTime { get; init; }
}

public sealed class OrderStats
{
    public long Total { get; set; }
    public long Completed { get; set; }
    public long Cancelled { get; set; }
    public double AvgPrice { get; set; }
    public double AvgDistance { get; set; }
}

public sealed class RatingStats
{
    public long Total { get; set; }
    public double AvgDriverRating { get; set; }
    public double AvgUserRating { get; set; }
}