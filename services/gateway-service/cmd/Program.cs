using PolesTaxi.GatewayService.App;

namespace PolesTaxi.GatewayService;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await Run.ExecuteAsync(args);
    }
}
