using PolesTaxi.DriverService.App.Db.Mongo;
using PolesTaxi.DriverService.Config;

namespace PolesTaxi.DriverService.App;

public static class Run
{
    public static async Task<int> ExecuteAsync(string[] args)
    {
        _ = args;
        var config = AppConfig.FromEnvironment();
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        MongoClient mongo;
        try
        {
            mongo = await MongoClient.New(config.MongoConnection, config.MongoDatabase, cts.Token);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"mongo init failed: {ex.Message}");
            return 1;
        }

        Console.WriteLine("mongodb ready");

        try { await Task.Delay(Timeout.Infinite, cts.Token); }
        catch (OperationCanceledException) { }

        Console.WriteLine("shutdown");
        return 0;
    }
}