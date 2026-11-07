using ClickHouse.Client.ADO;
using Dapper;
using PolesTaxi.AnalyticService.Entity.Service;
using PolesTaxi.Shared.Types;

namespace PolesTaxi.AnalyticService.Repository.ClickHouse;

public sealed class AnalyticClickHouseRepository : IAnalyticRepository
{
    private readonly string _connectionString;

    public AnalyticClickHouseRepository(string connectionString) => _connectionString = connectionString;

    public async Task InsertUserRegistered(UserRegisteredFact fact, CancellationToken cancellationToken)
    {
        using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(
            "INSERT INTO user_registered (user_id, role, event_time) VALUES (@UserId, @Role, @EventTime)",
            new { fact.UserId, Role = fact.Role.ToWire(), fact.EventTime });
    }

    public async Task InsertOrderCompleted(OrderCompletedFact fact, CancellationToken cancellationToken)
    {
        using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(
            "INSERT INTO order_completed (order_id, user_id, driver_id, taxi_type, price, distance, event_time) VALUES (@OrderId, @UserId, @DriverId, @TaxiType, @Price, @DistanceMeters, @EventTime)",
            new { fact.OrderId, fact.UserId, fact.DriverId, TaxiType = fact.TaxiType.ToWire(), fact.Price, fact.DistanceMeters, fact.EventTime });
    }

    public async Task InsertTripRated(TripRatedFact fact, CancellationToken cancellationToken)
    {
        using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(
            "INSERT INTO trip_rated (order_id, from_role, score, event_time) VALUES (@OrderId, @FromRole, @Score, @EventTime)",
            new { fact.OrderId, FromRole = fact.FromRole.ToWire(), fact.Score, fact.EventTime });
    }

    public async Task<OrderStats> GetOrderStats(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        
        var sql = @"
            SELECT 
                count() AS Total,
                countIf(1) AS Completed,
                0 AS Cancelled,
                if(isNaN(avg(price)), 0, avg(price)) AS AvgPrice,
                if(isNaN(avg(distance)), 0, avg(distance)) AS AvgDistance
            FROM order_completed
            WHERE event_time >= @from AND event_time <= @to";
        
        return await conn.QuerySingleAsync<OrderStats>(sql, new { from, to });
    }

    public async Task<RatingStats> GetRatingStats(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        
        var sql = @"
            SELECT 
                count() AS Total,
                avgIf(score, from_role = 'Driver') AS AvgDriverRating,
                avgIf(score, from_role = 'User') AS AvgUserRating
            FROM trip_rated
            WHERE event_time >= @from AND event_time <= @to";
        
        return await conn.QuerySingleAsync<RatingStats>(sql, new { from, to });
    }
}