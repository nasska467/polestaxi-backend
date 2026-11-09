using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace PolesTaxi.Shared.Hosting;

public static class Listen
{
    public static void DualPorts(KestrelServerOptions options, int httpPort, int grpcPort)
    {
        options.Listen(IPAddress.Any, httpPort, lo => lo.Protocols = HttpProtocols.Http1);
        options.Listen(IPAddress.Any, grpcPort, lo => lo.Protocols = HttpProtocols.Http2);
    }
}