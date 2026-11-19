namespace PolesTaxi.Shared.Hosting;

public static class Correlation
{
    public const string HeaderName = "X-Request-Id";
    private static readonly AsyncLocal<string?> Current = new();

    public static string? Id
    {
        get => Current.Value;
        set => Current.Value = value;
    }
}
