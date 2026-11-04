using PolesTaxi.WalletService.App;

namespace PolesTaxi.WalletService;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await Run.ExecuteAsync(args);
    }
}
