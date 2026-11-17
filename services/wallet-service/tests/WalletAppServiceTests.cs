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
        // Arrange (Подготовка)
        var userId = "user-123";
        var repo = Substitute.For<IWalletRepository>();
        
        // Настраиваем заглушку: при вызове ApplyMoney возвращаем фейковую транзакцию
        repo.ApplyMoney(
            Arg.Any<string>(), 
            Arg.Any<WalletTxType>(), 
            Arg.Any<decimal>(), 
            Arg.Any<string?>(), 
            Arg.Any<string?>(), 
            Arg.Any<CancellationToken>())
            .Returns(callInfo => new WalletTransaction
            {
                Id = Guid.NewGuid().ToString(),
                WalletId = "wallet-1",
                Type = callInfo.Arg<WalletTxType>(),
                Amount = callInfo.Arg<decimal>(),
                Status = WalletTxStatus.Completed,
                OrderId = null,
                // В методичке сказано передать null в тесте:
                IdempotencyKey = null, 
                CreatedAt = DateTime.UtcNow
            });

        // Создаем сервис с бонусом 0 (чтобы не мешал тесту)
        var service = new WalletAppService(repo, 0);

        // Act (Действие)
        // Списываем 40
        await service.ApplyMoney(userId, WalletTxType.Debit, 40, null, null, CancellationToken.None);
        
        // Зачисляем 100
        await service.ApplyMoney(userId, WalletTxType.Credit, 100, null, null, CancellationToken.None);

        // Assert (Проверка)
        // Проверяем, что метод ApplyMoney вызывался ровно 2 раза (списание и зачисление)
        await repo.Received(2).ApplyMoney(
            userId, 
            Arg.Any<WalletTxType>(), 
            Arg.Any<decimal>(), 
            Arg.Any<string?>(), 
            Arg.Any<string?>(), 
            Arg.Any<CancellationToken>());
    }
}