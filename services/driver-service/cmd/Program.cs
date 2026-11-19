using PolesTaxi.DriverService.App;

namespace PolesTaxi.DriverService;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await Run.ExecuteAsync(args);
    }
}
