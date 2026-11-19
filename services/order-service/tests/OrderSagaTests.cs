using NSubstitute;
using PolesTaxi.OrderService.Config;
using PolesTaxi.OrderService.Entity.Service;
using PolesTaxi.OrderService.Gateway;
using PolesTaxi.OrderService.Repository;
using PolesTaxi.OrderService.Service;
using PolesTaxi.Shared.Errors;
using PolesTaxi.Shared.Types;
using Xunit;
using Location = PolesTaxi.OrderService.Entity.Service.Location;

namespace PolesTaxi.OrderService.Tests;

public class OrderSagaTests
{
    private readonly IOrderRepository _orders;
    private readonly IWalletGateway _wallets;
    private readonly IDriverGateway _drivers;
    private readonly OrderAppService _service;
    private readonly PricingConfig _pricing;

    public OrderSagaTests()
    {
        _orders = Substitute.For<IOrderRepository>();
        _wallets = Substitute.For<IWalletGateway>();
        _drivers = Substitute.For<IDriverGateway>();
        _pricing = PricingConfig.FromEnvironment();
        _service = new OrderAppService(_orders, _wallets, _drivers, _pricing);
    }

    // Тест 1: Отказ списания оставляет индекс пустым
    [Fact]
    public async Task CreateOrder_WhenDebitFails_ShouldNotIndex()
    {
        var userId = "user-1";
        var pickup = new Location { Latitude = 55.75, Longitude = 37.62 };
        var dropoff = new Location { Latitude = 55.76, Longitude = 37.64 };

        _wallets.Debit(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Insufficient funds")));

        await Assert.ThrowsAsync<Exception>(() => 
            _service.CreateOrder(userId, pickup, dropoff, TaxiType.Economy, CancellationToken.None));

        await _orders.DidNotReceive().Index(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    // Тест 2: Цепочка Create → Accept → Start → Complete доходит до Completed
    [Fact]
    public async Task FullSaga_FromCreateToComplete_ShouldReachCompleted()
    {
        var userId = "user-1";
        var driverId = "driver-1";
        var pickup = new Location { Latitude = 55.75, Longitude = 37.62 };
        var dropoff = new Location { Latitude = 55.76, Longitude = 37.64 };
        var orderId = Guid.NewGuid().ToString("N");

        // Вспомогательная функция для создания Order с обязательными полями
        Order CreateOrderObj(string id, OrderStatus status, string driver = "") => new Order 
        { 
            Id = id, 
            UserId = userId, 
            DriverId = driver,
            Status = status,
            Pickup = pickup,
            Dropoff = dropoff
        };

        _orders.Index(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Order>());
        
        _orders.Update(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Order>());

        // 1. Create
        _orders.GetById(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateOrderObj(orderId, OrderStatus.Created));

        var created = await _service.CreateOrder(userId, pickup, dropoff, TaxiType.Economy, CancellationToken.None);
        Assert.Equal(OrderStatus.Created, created.Status);

        // 2. Accept
        _orders.GetById(created.Id, Arg.Any<CancellationToken>())
            .Returns(CreateOrderObj(created.Id, OrderStatus.Created));
        var accepted = await _service.Accept(created.Id, driverId, CancellationToken.None);
        Assert.Equal(OrderStatus.DriverAssigned, accepted.Status);

        // 3. Start
        _orders.GetById(created.Id, Arg.Any<CancellationToken>())
            .Returns(CreateOrderObj(created.Id, OrderStatus.DriverAssigned, driverId));
        var started = await _service.Start(created.Id, driverId, CancellationToken.None);
        Assert.Equal(OrderStatus.InProgress, started.Status);

        // 4. Complete
        _orders.GetById(created.Id, Arg.Any<CancellationToken>())
            .Returns(CreateOrderObj(created.Id, OrderStatus.InProgress, driverId));
        var completed = await _service.Complete(created.Id, driverId, CancellationToken.None);
        Assert.Equal(OrderStatus.Completed, completed.Status);
    }

    // Тест 3: Отмена вызывает Refund
    [Fact]
    public async Task Cancel_ShouldCallRefund()
    {
        var orderId = "order-1";
        var userId = "user-1";
        var pickup = new Location { Latitude = 55.75, Longitude = 37.62 };
        var dropoff = new Location { Latitude = 55.76, Longitude = 37.64 };

        var order = new Order 
        { 
            Id = orderId, 
            UserId = userId, 
            Status = OrderStatus.Created, 
            Price = 100,
            Pickup = pickup,
            Dropoff = dropoff
        };

        _orders.GetById(orderId, Arg.Any<CancellationToken>()).Returns(order);
        _orders.Update(Arg.Any<Order>(), Arg.Any<CancellationToken>()).Returns(callInfo => callInfo.Arg<Order>());

        await _service.Cancel(orderId, userId, CancellationToken.None);

        await _wallets.Received(1).Refund(userId, orderId, 100m, Arg.Any<CancellationToken>());
    }

    // Тест 4: Отказ водителя (заглушка, так как Decline не реализован)
    [Fact]
    public async Task Decline_ReturnsToCreated_Placeholder()
    {
        await Task.CompletedTask;
    }

    // Тест 5: Завершение из Created дает Conflict
    [Fact]
    public async Task Complete_FromCreated_ShouldThrowConflict()
    {
        var orderId = "order-1";
        var userId = "user-1";
        var pickup = new Location { Latitude = 55.75, Longitude = 37.62 };
        var dropoff = new Location { Latitude = 55.76, Longitude = 37.64 };

        var order = new Order 
        { 
            Id = orderId, 
            UserId = userId, 
            Status = OrderStatus.Created,
            Pickup = pickup,
            Dropoff = dropoff
        };

        _orders.GetById(orderId, Arg.Any<CancellationToken>()).Returns(order);

        var ex = await Assert.ThrowsAsync<AppException>(() => 
            _service.Complete(orderId, "driver-1", CancellationToken.None));
        
        Assert.Equal(ErrorCodes.Conflict, ex.Code);
    }

    // Тест 6: Оценка до Completed дает Conflict
    [Fact]
    public async Task RateOrder_BeforeCompleted_ShouldThrowConflict()
    {
        var orderId = "order-1";
        var userId = "user-1";
        var pickup = new Location { Latitude = 55.75, Longitude = 37.62 };
        var dropoff = new Location { Latitude = 55.76, Longitude = 37.64 };

        var order = new Order 
        { 
            Id = orderId, 
            UserId = userId, 
            Status = OrderStatus.Created,
            Pickup = pickup,
            Dropoff = dropoff
        };

        _orders.GetById(orderId, Arg.Any<CancellationToken>()).Returns(order);

        var ex = await Assert.ThrowsAsync<AppException>(() => 
            _service.RateOrder(orderId, userId, 5, CancellationToken.None));
        
        Assert.Equal(ErrorCodes.Conflict, ex.Code);
    }
}