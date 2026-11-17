using Grpc.Core;
using PolesTaxi.Contracts.Wallet.V1;
using PolesTaxi.Shared.Hosting;
using PolesTaxi.WalletService.Entity.Service;
using PolesTaxi.WalletService.Service;

namespace PolesTaxi.WalletService.Handler.Grpc;

public sealed class WalletGrpcHandler : PolesTaxi.Contracts.Wallet.V1.WalletService.WalletServiceBase
{
    private readonly IWalletAppService _service;

    public WalletGrpcHandler(IWalletAppService service) => _service = service;

    public override async Task<WalletResponse> CreateWallet(CreateWalletRequest request, ServerCallContext context)
    {
        var wallet = await _service.CreateWallet(request.UserId, request.Currency, context.CancellationToken);
        return ToResponse(wallet);
    }

    public override async Task<WalletResponse> GetMe(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
    {
        var userId = CallContext.RequireUserId(context);
        var wallet = await _service.GetByUserId(userId, context.CancellationToken);
        return ToResponse(wallet);
    }

    public override async Task<TransactionResponse> Debit(MoneyRequest request, ServerCallContext context)
    {
        var key = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey;
        
        // ИСПРАВЛЕНО: вызываем Debit, а не ApplyMoney
        var tx = await _service.Debit(
            request.UserId, 
            string.IsNullOrWhiteSpace(request.OrderId) ? null : request.OrderId, 
            (decimal)request.Amount, 
            key, 
            context.CancellationToken);
            
        return ToTxResponse(tx);
    }

    public override async Task<TransactionResponse> Credit(MoneyRequest request, ServerCallContext context)
    {
        var key = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey;
        var isRefund = key != null && key.Contains("refund", StringComparison.OrdinalIgnoreCase);

        // ИСПРАВЛЕНО: вызываем Credit, а не ApplyMoney
        var tx = await _service.Credit(
            request.UserId, 
            string.IsNullOrWhiteSpace(request.OrderId) ? null : request.OrderId, 
            (decimal)request.Amount, 
            isRefund, // <-- Передаем флаг возврата
            key, 
            context.CancellationToken);
            
        return ToTxResponse(tx);
    }

    private static WalletResponse ToResponse(Entity.Service.Wallet wallet) => new()
    {
        Id = wallet.Id,
        UserId = wallet.UserId,
        Balance = (double)wallet.Balance,
        Currency = wallet.Currency
    };

    private static TransactionResponse ToTxResponse(WalletTransaction tx) => new()
    {
        Id = tx.Id,
        WalletId = tx.WalletId,
        Type = tx.Type.ToString(),
        Amount = (double)tx.Amount,
        Status = tx.Status.ToString(),
        OrderId = tx.OrderId ?? string.Empty
    };
}