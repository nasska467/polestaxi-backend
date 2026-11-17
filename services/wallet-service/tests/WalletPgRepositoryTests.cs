using Npgsql;
using PolesTaxi.Shared.Errors;
using PolesTaxi.WalletService.Entity.Service;
using PolesTaxi.WalletService.Repository.Pg;
using Xunit;

namespace PolesTaxi.WalletService.Tests;

public class WalletPgRepositoryTests
{
    // ВНИМАНИЕ: Для работы этого теста нужна запущенная БД PostgreSQL.
    // Строка подключения должна совпадать с вашей (из docker-compose).
    private const string ConnectionString = "Host=localhost;Port=15432;Database=polestaxi_wallet;Username=polestaxi;Password=polestaxi";

    [Fact]
    public async Task Insufficient_funds_is_conflict()
    {
        // Arrange
        var repo = new WalletPgRepository(NpgsqlDataSource.Create(ConnectionString));
        var userId = Guid.NewGuid().ToString(); // Уникальный пользователь
        
        // Создаем кошелек с нулевым балансом
        await repo.CreateWallet(userId, "RUB", CancellationToken.None);

        // Act & Assert
        // Пытаемся списать 10 рублей с пустого кошелька
        var ex = await Assert.ThrowsAsync<AppException>(() => 
            repo.ApplyMoney(userId, WalletTxType.Debit, 10, null, $"test:debit:{userId}", CancellationToken.None));
            
        Assert.Equal("insufficient funds", ex.Message);
    }

    [Fact]
    public async Task Idempotent_debit_does_not_double_charge()
    {
        // Arrange
        var repo = new WalletPgRepository(NpgsqlDataSource.Create(ConnectionString));
        var userId = Guid.NewGuid().ToString();
        var key = $"ord:debit:{userId}";
        
        // Создаем кошелек
        await repo.CreateWallet(userId, "RUB", CancellationToken.None);
        
        // Зачисляем 100 рублей с уникальным ключом
        await repo.ApplyMoney(userId, WalletTxType.Credit, 100, null, $"signup:{userId}", CancellationToken.None);

        // Act
        // Дважды списываем 40 рублей с одним и тем же ключом
        var tx1 = await repo.ApplyMoney(userId, WalletTxType.Debit, 40, null, key, CancellationToken.None);
        var tx2 = await repo.ApplyMoney(userId, WalletTxType.Debit, 40, null, key, CancellationToken.None);

        // Assert
        Assert.Equal(tx1.Id, tx2.Id); // Должна вернуться та же транзакция
        
        // Проверяем баланс (должен быть 60)
        var wallet = await repo.GetByUserId(userId, CancellationToken.None);
        Assert.Equal(60, wallet.Balance);
    }
}
