using Grpc.Core;
using PolesTaxi.Contracts.Driver.V1;
using PolesTaxi.DriverService.Service;
using PolesTaxi.Shared.Hosting;
using PolesTaxi.Shared.Types;

namespace PolesTaxi.DriverService.Handler.Grpc;

public sealed class DriverGrpcHandler : PolesTaxi.Contracts.Driver.V1.DriverService.DriverServiceBase
{
    private readonly IDriverAppService _service;

    public DriverGrpcHandler(IDriverAppService service) => _service = service;

    public override async Task<DriverResponse> Register(RegisterDriverRequest request, ServerCallContext context)
    {
        var driver = await _service.Register(request.UserId, context.CancellationToken);
        return ToResponse(driver);
    }

    public override async Task<DriverResponse> GetMe(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
    {
        var userId = CallContext.RequireUserId(context);
        var driver = await _service.GetByUserId(userId, context.CancellationToken);
        return ToResponse(driver);
    }

    public override async Task<DriverResponse> SetStatus(SetStatusRequest request, ServerCallContext context)
    {
        var userId = CallContext.RequireUserId(context);
        // Явно приводим к нашему DriverStatus, чтобы не было неоднозначности
        var driver = await _service.SetStatus(userId, (PolesTaxi.Shared.Types.DriverStatus)(int)request.Status, context.CancellationToken);
        return ToResponse(driver);
    }

    public override async Task<FindAvailableResponse> FindAvailable(FindAvailableRequest request, ServerCallContext context)
    {
        var drivers = await _service.FindAvailable(
            // Явно приводим к нашему TaxiType
            (PolesTaxi.Shared.Types.TaxiType)(int)request.TaxiType,
            request.Lat,
            request.Lon,
            request.Limit,
            context.CancellationToken);

        var response = new FindAvailableResponse();
        response.Drivers.AddRange(drivers.Select(ToResponse));
        return response;
    }

    private static DriverResponse ToResponse(Entity.Service.Driver driver) => new()
    {
        Id = driver.Id,
        UserId = driver.UserId,
        TaxiType = (Contracts.Driver.V1.TaxiType)(int)driver.TaxiType,
        LicensePlate = driver.LicensePlate,
        Status = (Contracts.Driver.V1.DriverStatus)(int)driver.Status,
        Lat = driver.Location?.Latitude ?? 0,
        Lon = driver.Location?.Longitude ?? 0
    };
}