using PolesTaxi.AnalyticService.App.Db.ClickHouse;
using PolesTaxi.AnalyticService.Config;
using PolesTaxi.AnalyticService.Repository.ClickHouse;

namespace PolesTaxi.AnalyticService.App;

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
            var store = await ClickHouseStore.New(config.ClickHouseConnection, cts.Token);
            var repo = new AnalyticClickHouseRepository(config.ClickHouseConnection);
            Console.WriteLine("clickhouse ready");

            try { await Task.Delay(Timeout.Infinite, cts.Token); }
            catch (OperationCanceledException) { }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"clickhouse init failed: {ex.Message}");
            return 1;
        }

        Console.WriteLine("shutdown");
        return 0;
    }
}