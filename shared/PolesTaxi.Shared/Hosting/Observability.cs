using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;

namespace PolesTaxi.Shared.Hosting;

public static class Observability
{
    public static void Add(WebApplicationBuilder builder, string serviceName)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(o =>
        {
            o.IncludeScopes = true;
            o.TimestampFormat = "O";
        });
        var otlp = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation();
                t.AddHttpClientInstrumentation();
                if (!string.IsNullOrWhiteSpace(otlp))
                {
                    t.AddOtlpExporter(o => o.Endpoint = new Uri(otlp));
                }
            });
    }

    public static void UsePlatform(this WebApplication app, Func<CancellationToken, Task<bool>>? ready = null)
    {
        app.UseMiddleware<CorrelationMiddleware>();
        app.UseHttpMetrics();
        app.MapOps(ready);
    }

    public static void MapOps(this WebApplication app, Func<CancellationToken, Task<bool>>? ready = null)
    {
        app.MapGet("/healthz", () => Results.Text("ok"));
        app.MapGet("/readyz", async (CancellationToken ct) =>
        {
            try
            {
                if (ready is not null && !await ready(ct))
                    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                return Results.Text("ok");
            }
            catch
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        });
        app.MapMetrics("/metrics");
    }
}
