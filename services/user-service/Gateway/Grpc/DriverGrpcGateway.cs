using PolesTaxi.Contracts.Driver.V1;

namespace PolesTaxi.UserService.Gateway.Grpc;

public sealed class DriverGrpcGateway : IDriverGateway
{
    private readonly DriverService.DriverServiceClient _client;

    public DriverGrpcGateway(DriverService.DriverServiceClient client) => _client = client;

    public async Task RegisterDriver(string userId, CancellationToken cancellationToken)
    {
        await _client.RegisterAsync(new RegisterDriverRequest { UserId = userId }, cancellationToken: cancellationToken);
    }
}