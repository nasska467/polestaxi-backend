using Microsoft.AspNetCore.Http;
using PolesTaxi.AnalyticService.Repository;

namespace PolesTaxi.AnalyticService.Handler.Http;

public sealed class AnalyticHandler
{
    private readonly IAnalyticRepository _repo;

    public AnalyticHandler(IAnalyticRepository repo) => _repo = repo;

    public async Task<IResult> GetOrders(HttpContext context)
    {
        var (from, to) = ParseRange(context);
        var stats = await _repo.GetOrderStats(from, to, context.RequestAborted);
        return Results.Json(stats);
    }

    public async Task<IResult> GetRatings(HttpContext context)
    {
        var (from, to) = ParseRange(context);
        var stats = await _repo.GetRatingStats(from, to, context.RequestAborted);
        return Results.Json(stats);
    }

    public async Task<IResult> GetDashboard(HttpContext context)
    {
        var (from, to) = ParseRange(context);
        var orders = await _repo.GetOrderStats(from, to, context.RequestAborted);
        var ratings = await _repo.GetRatingStats(from, to, context.RequestAborted);
        return Results.Json(new { orders, ratings, activeUsers = 0, availableDrivers = 0 });
    }

    private static (DateTime from, DateTime to) ParseRange(HttpContext context)
    {
        var to = context.Request.Query.TryGetValue("to", out var toValue) && DateTime.TryParse(toValue, out var parsedTo)
            ? parsedTo : DateTime.UtcNow;
        var from = context.Request.Query.TryGetValue("from", out var fromValue) && DateTime.TryParse(fromValue, out var parsedFrom)
            ? parsedFrom : to.AddDays(-30);
        return (from, to);
    }
}