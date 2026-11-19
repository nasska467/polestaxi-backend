using PolesTaxi.OrderService.App;

namespace PolesTaxi.OrderService;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await Run.ExecuteAsync(args);
    }
}
