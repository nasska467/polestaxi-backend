using System.Text.Json;
using Confluent.Kafka;
using PolesTaxi.AnalyticService.Entity.Service;
using PolesTaxi.AnalyticService.Repository;
using PolesTaxi.Shared.Consts;
using PolesTaxi.Shared.Types;

namespace PolesTaxi.AnalyticService.Handler.Kafka;

public sealed class KafkaHandler
{
    private readonly IAnalyticRepository _repo;
    private readonly string _brokers;

    public KafkaHandler(IAnalyticRepository repo, string brokers)
    {
        _repo = repo;
        _brokers = brokers;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _brokers,
            GroupId = "analytic-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(new[] { KafkaTopics.UserRegistered, KafkaTopics.OrderStatusChanged, KafkaTopics.TripRated });
        Console.WriteLine("kafka consumer subscribed");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromMilliseconds(250));
                if (result?.Message?.Value is null) continue;

                await Dispatch(result.Topic, result.Message.Value, cancellationToken);
            }
            catch (ConsumeException ex)
            {
                Console.WriteLine($"kafka error: {ex.Error.Reason}");
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        consumer.Close();
    }

    private async Task Dispatch(string topic, string payload, CancellationToken cancellationToken)
    {
        switch (topic)
        {
            case KafkaTopics.UserRegistered:
                var userEvent = JsonSerializer.Deserialize<UserRegisteredPayload>(payload);
                if (userEvent is not null)
                    await _repo.InsertUserRegistered(new UserRegisteredFact
                    {
                        UserId = userEvent.UserId,
                        Role = Role.User,
                        EventTime = DateTime.UtcNow
                    }, cancellationToken);
                break;

            case KafkaTopics.OrderStatusChanged:
                var orderEvent = JsonSerializer.Deserialize<OrderStatusPayload>(payload);
                if (orderEvent?.NewStatus == "Completed")
                {
                    await _repo.InsertOrderCompleted(new OrderCompletedFact
                    {
                        OrderId = orderEvent.OrderId,
                        UserId = orderEvent.UserId,
                        DriverId = orderEvent.DriverId,
                        TaxiType = TaxiType.Economy,
                        Price = (decimal)orderEvent.Price,
                        DistanceMeters = orderEvent.DistanceMeters,
                        EventTime = DateTime.UtcNow
                    }, cancellationToken);
                }
                break;

            case KafkaTopics.TripRated:
                var rateEvent = JsonSerializer.Deserialize<TripRatedPayload>(payload);
                if (rateEvent is not null)
                    await _repo.InsertTripRated(new TripRatedFact
                    {
                        OrderId = rateEvent.OrderId,
                        FromRole = Role.User,
                        Score = rateEvent.Score,
                        EventTime = DateTime.UtcNow
                    }, cancellationToken);
                break;
        }
    }

    private sealed class UserRegisteredPayload
    {
        public string UserId { get; set; } = string.Empty;
    }

    private sealed class OrderStatusPayload
    {
        public string OrderId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string DriverId { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public double Price { get; set; }
        public long DistanceMeters { get; set; }
    }

    private sealed class TripRatedPayload
    {
        public string OrderId { get; set; } = string.Empty;
        public int Score { get; set; }
    }
}