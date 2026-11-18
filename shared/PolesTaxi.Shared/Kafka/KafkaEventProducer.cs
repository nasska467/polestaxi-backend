using System.Text;
using Confluent.Kafka;
using PolesTaxi.Shared.Hosting;

namespace PolesTaxi.Shared.Kafka;

public sealed class KafkaEventProducer : IDisposable
{
    private readonly IProducer<string, byte[]>? _producer;

    public KafkaEventProducer(string? brokers)
    {
        if (string.IsNullOrWhiteSpace(brokers))
            return;

        var config = new ProducerConfig
        {
            BootstrapServers = brokers,
            MessageTimeoutMs = 15000
        };
        _producer = new ProducerBuilder<string, byte[]>(config).Build();
    }

    public async Task PublishAsync(string topic, string key, byte[] payload, CancellationToken cancellationToken = default)
    {
        if (_producer is null)
        {
            Console.WriteLine($"kafka skipped topic={topic} key={key} bytes={payload.Length}");
            return;
        }

        var message = new Message<string, byte[]>
        {
            Key = key,
            Value = payload
        };

        if (!string.IsNullOrWhiteSpace(Correlation.Id))
        {
            message.Headers = new Headers
            {
                { "x-request-id", Encoding.UTF8.GetBytes(Correlation.Id) }
            };
        }

        await _producer.ProduceAsync(topic, message, cancellationToken);
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(5));
        _producer?.Dispose();
    }
}
