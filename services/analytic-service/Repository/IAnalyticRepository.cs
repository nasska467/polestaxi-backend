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