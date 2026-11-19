using Microsoft.AspNetCore.Http;

namespace PolesTaxi.Shared.Hosting;

public sealed class CorrelationMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers[Correlation.HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(id))
        {
            id = Guid.NewGuid().ToString("N");
        }

        Correlation.Id = id;
        context.Request.Headers[Correlation.HeaderName] = id;
        context.Response.Headers[Correlation.HeaderName] = id;

        await _next(context);
    }
}
