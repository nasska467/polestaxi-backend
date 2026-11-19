using Grpc.Core;
using PolesTaxi.Contracts.Driver.V1;
using PolesTaxi.OrderService.Gateway;

namespace PolesTaxi.OrderService.Gateway.Grpc;

public sealed class DriverGrpcGateway : IDriverGateway
{
    private readonly DriverService.DriverServiceClient _client;

    public DriverGrpcGateway(DriverService.DriverServiceClient client)
    {
        _client = client;
    }

    public async Task AcceptTrip(string driverId, string orderId, CancellationToken cancellationToken)
    {
        var request = new TripActionRequest
        {
            UserId = driverId,
            OrderId = orderId
        };

        await _client.AcceptTripAsync(request, cancellationToken: cancellationToken);
    }

    public async Task StartTrip(string driverId, string orderId, CancellationToken cancellationToken)
    {
        var request = new TripActionRequest
        {
            UserId = driverId,
            OrderId = orderId
        };

        await _client.StartTripAsync(request, cancellationToken: cancellationToken);
    }

    public async Task CompleteTrip(string driverId, string orderId, CancellationToken cancellationToken)
    {
        var request = new TripActionRequest
        {
            UserId = driverId,
            OrderId = orderId
        };

        await _client.CompleteTripAsync(request, cancellationToken: cancellationToken);
    }
}