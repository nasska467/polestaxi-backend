using PolesTaxi.UserService.App;

namespace PolesTaxi.UserService;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await Run.ExecuteAsync(args);
    }
}