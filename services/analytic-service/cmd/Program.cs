using PolesTaxi.AnalyticService.App;

namespace PolesTaxi.AnalyticService;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await Run.ExecuteAsync(args);
    }
}
