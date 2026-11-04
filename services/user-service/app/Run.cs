namespace PolesTaxi.UserService.App;

public static class Run
{
    public static Task<int> ExecuteAsync(string[] args)
    {
        _ = args;
        Console.WriteLine("PolesTaxi.UserService skeleton (phase 1: no HTTP/gRPC yet)");
        return Task.FromResult(0);
    }
}