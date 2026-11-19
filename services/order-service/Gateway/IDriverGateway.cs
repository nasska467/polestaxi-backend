namespace PolesTaxi.OrderService.Gateway;

public interface IDriverGateway
{
    Task AcceptTrip(string driverId, string orderId, CancellationToken cancellationToken);
    Task StartTrip(string driverId, string orderId, CancellationToken cancellationToken);
    Task CompleteTrip(string driverId, string orderId, CancellationToken cancellationToken);
}