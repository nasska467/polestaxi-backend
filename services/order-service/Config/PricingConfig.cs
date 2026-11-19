namespace PolesTaxi.OrderService.Config;

public sealed class PricingConfig
{
    public decimal Base { get; init; } = 100m;
    public decimal PerKm { get; init; } = 15m;
    public decimal PerMin { get; init; } = 0m;
    public decimal ComfortMultiplier { get; init; } = 1.4m;
    public decimal BusinessMultiplier { get; init; } = 2m;
    public decimal Surge { get; init; } = 1m;

    public static PricingConfig FromEnvironment()
    {
        return new PricingConfig
        {
            Base = ParseEnv("PRICE_BASE", 100m),
            PerKm = ParseEnv("PRICE_PER_KM", 15m),
            PerMin = ParseEnv("PRICE_PER_MIN", 0m),
            ComfortMultiplier = ParseEnv("PRICE_COMFORT_MULT", 1.4m),
            BusinessMultiplier = ParseEnv("PRICE_BUSINESS_MULT", 2m),
            Surge = ParseEnv("SURGE", 1m)
        };
    }

    private static decimal ParseEnv(string key, decimal defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return decimal.TryParse(value, out var result) ? result : defaultValue;
    }
}