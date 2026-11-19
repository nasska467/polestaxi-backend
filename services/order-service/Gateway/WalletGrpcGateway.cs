using Grpc.Core;
using PolesTaxi.Contracts.Wallet.V1;
using PolesTaxi.OrderService.Gateway;

namespace PolesTaxi.OrderService.Gateway.Grpc;

public sealed class WalletGrpcGateway : IWalletGateway
{
    private readonly WalletService.WalletServiceClient _client;

    public WalletGrpcGateway(WalletService.WalletServiceClient client)
    {
        _client = client;
    }

    public async Task Debit(string userId, string orderId, decimal amount, CancellationToken cancellationToken)
    {
        var request = new MoneyRequest
        {
            UserId = userId,
            OrderId = orderId,
            Amount = (double)amount,
            IdempotencyKey = $"{orderId}:debit"
        };

        await _client.DebitAsync(request, cancellationToken: cancellationToken);
    }

    public async Task Refund(string userId, string orderId, decimal amount, CancellationToken cancellationToken)
    {
        var request = new MoneyRequest
        {
            UserId = userId,
            OrderId = orderId,
            Amount = (double)amount,
            IdempotencyKey = $"{orderId}:refund"
        };

        await _client.CreditAsync(request, cancellationToken: cancellationToken);
    }
}