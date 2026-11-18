using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace PolesTaxi.Shared.Hosting;

public static class GrpcResilience
{
    public static IHttpClientBuilder AddPolesTaxiResilience(this IHttpClientBuilder builder)
    {
        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(8) })
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<IOException>()
                    .Handle<TimeoutRejectedException>()
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(20),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(15),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<IOException>()
            })
            .Build();

        return builder
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(15))
            .AddHttpMessageHandler(() => new ResilienceDelegatingHandler(pipeline));
    }

    private sealed class ResilienceDelegatingHandler : DelegatingHandler
    {
        private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

        public ResilienceDelegatingHandler(ResiliencePipeline<HttpResponseMessage> pipeline) =>
            _pipeline = pipeline;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            _pipeline.ExecuteAsync(async ct => await base.SendAsync(request, ct), cancellationToken).AsTask();
    }
}
