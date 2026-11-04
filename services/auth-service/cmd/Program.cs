using PolesTaxi.AuthService.App;

namespace PolesTaxi.AuthService;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await Run.ExecuteAsync(args);
    }
}
