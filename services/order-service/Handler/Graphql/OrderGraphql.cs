using HotChocolate;
using HotChocolate.Types;
using PolesTaxi.OrderService.Entity.Service;
using PolesTaxi.OrderService.Service;
using PolesTaxi.Shared.Hosting;
using PolesTaxi.Shared.Types;
using Location = PolesTaxi.OrderService.Entity.Service.Location;

namespace PolesTaxi.OrderService.Handler.Graphql;

public sealed class OrderQueries
{
    public async Task<Order?> GetOrder(string id, [Service] IOrderAppService service, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[GraphQL] GetOrder called with id={id}");
        try
        {
            var order = await service.GetById(id, cancellationToken);
            Console.WriteLine($"[GraphQL] Order found: {order?.Id ?? "null"}");
            return order;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GraphQL] Error: {ex.Message}");
            throw new GraphQLException(ex.Message);
        }
    }
}

public sealed class OrderMutations
{
    public async Task<Order> CreateOrder(
        double pickupLat, double pickupLon, string pickupAddress,
        double dropoffLat, double dropoffLon, string dropoffAddress,
        TaxiType taxiType,
        [Service] IOrderAppService service,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext?.Request.Headers["x-user-id"].ToString();
        if (string.IsNullOrWhiteSpace(userId))
            throw new GraphQLException("x-user-id header is missing");

        var pickup = new Location { Latitude = pickupLat, Longitude = pickupLon, Address = pickupAddress };
        var dropoff = new Location { Latitude = dropoffLat, Longitude = dropoffLon, Address = dropoffAddress };

        return await service.CreateOrder(userId, pickup, dropoff, taxiType, cancellationToken);
    }

    public async Task<Order> CancelOrder(
        string id,
        [Service] IOrderAppService service,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext?.Request.Headers["x-user-id"].ToString();
        if (string.IsNullOrWhiteSpace(userId))
            throw new GraphQLException("x-user-id header is missing");

        return await service.Cancel(id, userId, cancellationToken);
    }

    public async Task<Order> RateOrder(
        string id,
        int score,
        [Service] IOrderAppService service,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext?.Request.Headers["x-user-id"].ToString();
        if (string.IsNullOrWhiteSpace(userId))
            throw new GraphQLException("x-user-id header is missing");

        return await service.RateOrder(id, userId, score, cancellationToken);
    }
}