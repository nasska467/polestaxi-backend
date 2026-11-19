using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace PolesTaxi.Shared.Kafka;

public static class KafkaAdmin
{
    public static async Task EnsureTopics(string brokers, IEnumerable<string> topics)
    {
        if (string.IsNullOrWhiteSpace(brokers))
            return;

        using var admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = brokers
        }).Build();

        var specs = topics
            .Distinct()
            .Select(name => new TopicSpecification
            {
                Name = name,
                NumPartitions = 1,
                ReplicationFactor = 1
            })
            .ToList();

        try
        {
            await admin.CreateTopicsAsync(specs);
        }
        catch (CreateTopicsException ex)
        {
            if (ex.Results.Any(r => r.Error.IsError && r.Error.Code != ErrorCode.TopicAlreadyExists))
                throw;
        }
    }
}
