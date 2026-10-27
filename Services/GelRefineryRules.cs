namespace taums_slime_ranching.Services;

internal sealed class GelRefineryRules
{
    private GelRefineryRules(int fuelCost, int durationDays)
    {
        this.FuelCost = fuelCost;
        this.DurationDays = durationDays;
    }

    public int FuelCost { get; }
    public int DurationDays { get; }

    public static GelRefineryRules FromConfig(ModConfig config) => new(
        Math.Clamp(config.GelRefinerySlimeFuelCost, 1, 999_999),
        Math.Clamp(config.GelRefineryDurationDays, 1, 28));
}
