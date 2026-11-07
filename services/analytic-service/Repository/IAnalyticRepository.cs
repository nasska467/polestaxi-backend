using PolesTaxi.AnalyticService.Entity.Service;

namespace PolesTaxi.AnalyticService.Repository;

public interface IAnalyticRepository
{
    Task InsertUserRegistered(UserRegisteredFact fact, CancellationToken cancellationToken);
    Task InsertOrderCompleted(OrderCompletedFact fact, CancellationToken cancellationToken);
    Task InsertTripRated(TripRatedFact fact, CancellationToken cancellationToken);
    Task<OrderStats> GetOrderStats(DateTime from, DateTime to, CancellationToken cancellationToken);
    Task<RatingStats> GetRatingStats(DateTime from, DateTime to, CancellationToken cancellationToken);
}

public sealed class OrderStats
{
    public long Total { get; set; }
    public long Completed { get; set; }
    public long Cancelled { get; set; }
    public decimal AvgPrice { get; set; }
    public double AvgDistance { get; set; }
}

public sealed class RatingStats
{
    public long Total { get; set; }
    public double AvgDriverRating { get; set; }
    public double AvgUserRating { get; set; }
}