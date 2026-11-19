using Grpc.Core;
using PolesTaxi.Contracts.Order.V1;
using PolesTaxi.OrderService.Entity.Service;
using PolesTaxi.OrderService.Service;
using PolesTaxi.Shared.Hosting;
using PolesTaxi.Shared.Types;
using Location = PolesTaxi.OrderService.Entity.Service.Location;
using TaxiType = PolesTaxi.Shared.Types.TaxiType;
using OrderStatus = PolesTaxi.Shared.Types.OrderStatus;

namespace PolesTaxi.OrderService.Handler.Grpc;

public sealed class OrderGrpcHandler : PolesTaxi.Contracts.Order.V1.OrderService.OrderServiceBase
{
    private readonly IOrderAppService _service;

    public OrderGrpcHandler(IOrderAppService service) => _service = service;

    public override async Task<OrderResponse> CreateOrder(CreateOrderRequest request, ServerCallContext context)
    {
        var userId = CallContext.RequireUserId(context);
        var pickup = new Location { Latitude = request.PickupLat, Longitude = request.PickupLon, Address = request.PickupAddress };
        var dropoff = new Location { Latitude = request.DropoffLat, Longitude = request.DropoffLon, Address = request.DropoffAddress };
        var order = await _service.CreateOrder(userId, pickup, dropoff, (TaxiType)(int)request.TaxiType, context.CancellationToken);
        return ToResponse(order);
    }

    public override async Task<OrderResponse> GetOrder(GetOrderRequest request, ServerCallContext context)
    {
        var order = await _service.GetById(request.Id, context.CancellationToken);
        return ToResponse(order);
    }

    public override async Task<OrderResponse> CancelOrder(GetOrderRequest request, ServerCallContext context)
    {
        var userId = CallContext.RequireUserId(context);
        var order = await _service.Cancel(request.Id, userId, context.CancellationToken);
        return ToResponse(order);
    }

    private static OrderResponse ToResponse(Entity.Service.Order order) => new()
    {
        Id = order.Id,
        UserId = order.UserId,
        DriverId = order.DriverId ?? string.Empty,
        TaxiType = (Contracts.Order.V1.TaxiType)(int)order.TaxiType,
        Status = (Contracts.Order.V1.OrderStatus)(int)order.Status,
        Price = order.Price,
        Surge = order.Surge,
        DistanceMeters = order.DistanceMeters,
        DurationSeconds = order.DurationSeconds,
        UserRating = order.UserRating ?? 0,
        DriverRating = order.DriverRating ?? 0,
        Comment = order.Comment ?? string.Empty
    };
}