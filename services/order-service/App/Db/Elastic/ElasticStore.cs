using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Transport;

namespace PolesTaxi.OrderService.App.Db.Elastic;

public sealed class ElasticStore
{
    public ElasticsearchClient Client { get; }
    public string IndexName { get; }

    private ElasticStore(ElasticsearchClient client, string indexName)
    {
        Client = client;
        IndexName = indexName;
    }

    public static async Task<ElasticStore> New(string uri, string indexName, CancellationToken cancellationToken)
    {
        var httpUri = uri.Replace("https://", "http://");
        var settings = new ElasticsearchClientSettings(new Uri(httpUri))
            .DefaultIndex(indexName)
            .ThrowExceptions(false);

        var client = new ElasticsearchClient(settings);
        var store = new ElasticStore(client, indexName);
        await store.EnsureIndex(cancellationToken);
        return store;
    }

    private async Task EnsureIndex(CancellationToken cancellationToken)
    {
        var exists = await Client.Indices.ExistsAsync(IndexName, cancellationToken);
        if (exists.Exists) return;

        var request = new CreateIndexRequest(IndexName)
        {
            Mappings = new TypeMapping
            {
                Properties = new Properties
                {
                    { "id", new KeywordProperty() },
                    { "userId", new KeywordProperty() },
                    { "driverId", new KeywordProperty() },
                    { "taxiType", new KeywordProperty() },
                    { "status", new KeywordProperty() },
                    { "pickup", new ObjectProperty { Properties = new Properties { { "lat", new FloatNumberProperty() }, { "lon", new FloatNumberProperty() }, { "address", new TextProperty() } } } },
                    { "dropoff", new ObjectProperty { Properties = new Properties { { "lat", new FloatNumberProperty() }, { "lon", new FloatNumberProperty() }, { "address", new TextProperty() } } } },
                    { "distanceMeters", new LongNumberProperty() },
                    { "durationSeconds", new LongNumberProperty() },
                    { "price", new FloatNumberProperty() },
                    { "surge", new FloatNumberProperty() },
                    { "userRating", new ByteNumberProperty() },
                    { "driverRating", new ByteNumberProperty() },
                    { "comment", new TextProperty() },
                    { "createdAt", new DateProperty() },
                    { "assignedAt", new DateProperty() },
                    { "startedAt", new DateProperty() },
                    { "finishedAt", new DateProperty() }
                }
            }
        };

        var created = await Client.Indices.CreateAsync(request, cancellationToken);
        if (!created.IsValidResponse)
            throw new InvalidOperationException($"elasticsearch mapping failed: {created.DebugInformation}");
    }
}