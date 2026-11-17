using NSubstitute;
using PolesTaxi.Shared.Errors;
using PolesTaxi.Shared.Types;
using PolesTaxi.UserService.Entity.Service;
using PolesTaxi.UserService.Gateway;
using PolesTaxi.UserService.Repository;
using PolesTaxi.UserService.Service;
using Xunit;

namespace PolesTaxi.UserService.Tests;

public class UserRegisterTests
{
    [Fact]
    public async Task Register_Compensates_When_Wallet_Fails()
    {
        // Arrange (Подготовка)
        var users = Substitute.For<IUserRepository>();
        var drivers = Substitute.For<IDriverGateway>();
        var wallets = Substitute.For<IWalletGateway>();

        // Заполняем ВСЕ обязательные поля (required)
        var user = new User 
        { 
            Id = "user-123", 
            Email = "ann@test.com",
            Name = "Ann",
            Phone = "+7999",
            PasswordHash = "hash",
            Roles = new[] { Role.User },
            RecentRatings = Array.Empty<double>()
        };
        
        users.Create(Arg.Any<User>(), Arg.Any<CancellationToken>())
             .Returns(user);

        wallets.CreateWallet(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Task.FromException(new Exception("Wallet Service is unavailable")));

        var service = new UserAppService(users, drivers, wallets);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            service.Register("Ann", "+7999", "ann@test.com", "password", Role.User, CancellationToken.None));

        // Проверяем, что компенсация сработала
        await users.Received(1).SoftDelete("user-123", Arg.Any<CancellationToken>());
    }
}