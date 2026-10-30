namespace taums_slime_ranching.Services;

internal sealed class SlimePrinterRules
{
    public const int MinimumSlimeFuelCost = 10;
    public const int MaximumSlimeFuelCost = 999_999;

    private SlimePrinterRules(int fuelCost, int durationDays)
    {
        this.FuelCost = fuelCost;
        this.DurationDays = durationDays;
    }

    public int FuelCost { get; }
    public int DurationDays { get; }

    public static string FormatInGameDays(int days) => days == 1
        ? "1 in-game day"
        : $"{days} in-game days";

    public static SlimePrinterRules FromConfig(ModConfig config) => new(
        Math.Clamp(config.SlimePrinterFuelCost, MinimumSlimeFuelCost, MaximumSlimeFuelCost),
        Math.Clamp(config.SlimePrinterDurationDays, 1, 28));

    public static bool NormalizeFuelCost(ModConfig config)
    {
        int normalizedCost = Math.Clamp(config.SlimePrinterFuelCost, MinimumSlimeFuelCost, MaximumSlimeFuelCost);
        if (config.SlimePrinterFuelCost == normalizedCost)
            return false;

        config.SlimePrinterFuelCost = normalizedCost;
        return true;
    }
}
