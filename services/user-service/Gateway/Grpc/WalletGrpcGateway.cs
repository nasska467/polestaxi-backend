using PolesTaxi.Contracts.Wallet.V1;

namespace PolesTaxi.UserService.Gateway.Grpc;

public sealed class WalletGrpcGateway : IWalletGateway
{
    private readonly WalletService.WalletServiceClient _client;

    public WalletGrpcGateway(WalletService.WalletServiceClient client) => _client = client;

    public async Task CreateWallet(string userId, CancellationToken cancellationToken)
    {
        await _client.CreateWalletAsync(new CreateWalletRequest { UserId = userId, Currency = "RUB" }, cancellationToken: cancellationToken);
    }
}