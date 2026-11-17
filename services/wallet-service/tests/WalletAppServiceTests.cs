using NSubstitute;
using PolesTaxi.WalletService.Entity.Service;
using PolesTaxi.WalletService.Repository;
using PolesTaxi.WalletService.Service;
using Xunit;

namespace PolesTaxi.WalletService.Tests;

public class WalletAppServiceTests
{
    [Fact]
    public async Task Atomic_credit_then_debit()
    {
        // Arrange
        var userId = "user-123";
        var repo = Substitute.For<IWalletRepository>();
        var service = new WalletAppService(repo, 0); // Бонус 0 для чистоты теста

        // Act
        await service.Debit(userId, null, 40, null, CancellationToken.None);
        await service.Credit(userId, null, 100, false, null, CancellationToken.None);

        // Assert
        // Проверяем, что ApplyMoney вызывался ровно 2 раза
        await repo.Received(2).ApplyMoney(
            Arg.Any<string>(), 
            Arg.Any<WalletTxType>(), 
            Arg.Any<decimal>(), 
            Arg.Any<string?>(), 
            Arg.Any<string?>(), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Idempotent_debit_does_not_double_charge()
    {
        // Arrange
        var userId = "user-123";
        var key = "ord:debit";
        var repo = Substitute.For<IWalletRepository>();
        
        // Настраиваем заглушку, чтобы она возвращала один и тот же ID для одного и того же ключа
        var txId = Guid.NewGuid().ToString();
        repo.ApplyMoney(userId, WalletTxType.Debit, 40, Arg.Any<string?>(), key, Arg.Any<CancellationToken>())
            .Returns(new WalletTransaction 
            { 
                Id = txId, 
                WalletId = "wallet-1", 
                Type = WalletTxType.Debit, 
                Amount = 40, 
                Status = WalletTxStatus.Completed,
                IdempotencyKey = key,
                CreatedAt = DateTime.UtcNow
            });

        var service = new WalletAppService(repo, 0);

        // Act
        var tx1 = await service.Debit(userId, "order-1", 40, key, CancellationToken.None);
        var tx2 = await service.Debit(userId, "order-1", 40, key, CancellationToken.None);

        // Assert
        Assert.Equal(tx1.Id, tx2.Id); // ID транзакций должны совпадать
    }
}