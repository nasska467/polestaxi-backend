using PolesTaxi.OrderService.Entity.Service;

namespace PolesTaxi.OrderService.Repository;

public interface IOrderRepository
{
    Task<Order> Index(Order order, CancellationToken cancellationToken);
    Task<Order> GetById(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> Search(OrderSearch query, CancellationToken cancellationToken);
    Task<Order> Update(Order order, CancellationToken cancellationToken);
}