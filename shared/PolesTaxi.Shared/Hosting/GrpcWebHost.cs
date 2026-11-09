using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace PolesTaxi.Shared.Hosting;

public static class GrpcWebHost
{
    public static IServiceCollection AddPolesTaxiGrpc(this IServiceCollection services)
    {
        services.AddSingleton<AppExceptionInterceptor>();
        services.AddGrpc(options => options.Interceptors.Add<AppExceptionInterceptor>())
                .AddJsonTranscoding();
        return services;
    }

    public static void MapHealth(this WebApplication app)
    {
        app.MapGet("/healthz", () => Results.Text("ok"));
    }
}