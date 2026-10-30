namespace taums_slime_ranching.Services;

internal sealed class GelRefineryRules
{
    public const int MinimumSlimeFuelCost = 10;
    public const int MaximumSlimeFuelCost = 999_999;

    private GelRefineryRules(int fuelCost, int durationDays)
    {
        this.FuelCost = fuelCost;
        this.DurationDays = durationDays;
    }

    public int FuelCost { get; }
    public int DurationDays { get; }

    public static string FormatInGameDays(int days) => days == 1
        ? "1 in-game day"
        : $"{days} in-game days";

    public static GelRefineryRules FromConfig(ModConfig config) => new(
        Math.Clamp(config.GelRefinerySlimeFuelCost, MinimumSlimeFuelCost, MaximumSlimeFuelCost),
        Math.Clamp(config.GelRefineryDurationDays, 1, 28));

    public static bool NormalizeFuelCost(ModConfig config)
    {
        int normalizedCost = Math.Clamp(config.GelRefinerySlimeFuelCost, MinimumSlimeFuelCost, MaximumSlimeFuelCost);
        if (config.GelRefinerySlimeFuelCost == normalizedCost)
            return false;

        config.GelRefinerySlimeFuelCost = normalizedCost;
        return true;
    }
}
