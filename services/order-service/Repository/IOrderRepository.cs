using PolesTaxi.OrderService.Entity.Service;

namespace PolesTaxi.OrderService.Repository;

public interface IOrderRepository
{
    Task<Order> Index(Order order, CancellationToken cancellationToken);
    Task<Order> GetById(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> Search(OrderSearch query, CancellationToken cancellationToken);
    Task<Order> Update(Order order, CancellationToken cancellationToken);
}

public sealed class OrderSearch
{
    public Shared.Types.OrderStatus? Status { get; init; }
    public string? UserId { get; init; }
    public string? DriverId { get; init; }
    public int PageSize { get; init; } = 20;
}