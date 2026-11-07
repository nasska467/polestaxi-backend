using PolesTaxi.WalletService.App.Db.Pg;
using PolesTaxi.WalletService.Config;

namespace PolesTaxi.WalletService.App;

public static class Run
{
    public static async Task<int> ExecuteAsync(string[] args)
    {
        _ = args;
        var config = AppConfig.FromEnvironment();
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        try
        {
            using var pg = PgClient.New(config.PostgresConnection);
            Console.WriteLine("postgres ready");

            try { await Task.Delay(Timeout.Infinite, cts.Token); }
            catch (OperationCanceledException) { }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"wallet store init failed: {ex.Message}");
            return 1;
        }

        Console.WriteLine("shutdown");
        return 0;
    }
}