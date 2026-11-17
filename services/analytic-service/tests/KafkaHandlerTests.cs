using System.Text.Json;
using NSubstitute;
using PolesTaxi.AnalyticService.Entity.Service;
using PolesTaxi.AnalyticService.Handler.Kafka;
using PolesTaxi.AnalyticService.Repository;
using PolesTaxi.Shared.Consts;
using PolesTaxi.Shared.Types;
using Xunit;

namespace PolesTaxi.AnalyticService.Tests;

public sealed class KafkaHandlerTests
{
    [Fact]
    public async Task Dispatch_completed_order_records_fact()
    {
        var repo = Substitute.For<IAnalyticRepository>();
        var payload = JsonSerializer.Serialize(new
        {
            OrderId = "ord-1",
            UserId = "u-1",
            DriverId = "d-1",
            NewStatus = "Completed",
            Price = 125.5,
            DistanceMeters = 1500L,
            TaxiType = (int)TaxiType.Economy
        });

        await KafkaHandler.Dispatch(repo, KafkaTopics.OrderStatusChanged, payload, CancellationToken.None);

        await repo.Received(1).InsertOrderCompleted(
            Arg.Is<OrderCompletedFact>(f => f.OrderId == "ord-1" && f.Price == 125.5m && f.DistanceMeters == 1500L),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Dispatch_created_order_does_not_record_fact()
    {
        var repo = Substitute.For<IAnalyticRepository>();
        var payload = JsonSerializer.Serialize(new
        {
            OrderId = "ord-2",
            UserId = "u-1",
            DriverId = "",
            NewStatus = "Created",
            Price = 100.0,
            DistanceMeters = 1000L,
            TaxiType = (int)TaxiType.Economy
        });

        await KafkaHandler.Dispatch(repo, KafkaTopics.OrderStatusChanged, payload, CancellationToken.None);

        await repo.DidNotReceive().InsertOrderCompleted(
            Arg.Any<OrderCompletedFact>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Dispatch_user_registered_and_trip_rated_record_facts()
    {
        var repo = Substitute.For<IAnalyticRepository>();

        var regPayload = JsonSerializer.Serialize(new { UserId = "u-1" });
        await KafkaHandler.Dispatch(repo, KafkaTopics.UserRegistered, regPayload, CancellationToken.None);

        var ratePayload = JsonSerializer.Serialize(new { OrderId = "ord-1", Score = 5 });
        await KafkaHandler.Dispatch(repo, KafkaTopics.TripRated, ratePayload, CancellationToken.None);

        await repo.Received(1).InsertUserRegistered(
            Arg.Is<UserRegisteredFact>(f => f.UserId == "u-1"),
            Arg.Any<CancellationToken>());

        await repo.Received(1).InsertTripRated(
            Arg.Is<TripRatedFact>(f => f.OrderId == "ord-1" && f.Score == 5),
            Arg.Any<CancellationToken>());
    }
}
