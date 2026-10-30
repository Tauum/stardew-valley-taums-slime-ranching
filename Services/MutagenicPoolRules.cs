namespace taums_slime_ranching.Services;

/// <summary>Approved fixed chance, timing, contribution, and cosmetic mutation rules.</summary>
internal sealed class MutagenicPoolRules
{
    public const int DurationDays = 2;
    public const int SlimePerContributionPoint = 10;
    public const double BaseChance = 0.10;
    public const double PityPerFailure = 0.01;
    public const double MaximumChance = 1.0;
    public const double ContributionPoint = 0.01;

    public static readonly IReadOnlyList<string> MutationTraits = new[]
    {
        "Starry", "Spotted", "Glowing", "Crystalline", "Mossy", "Bubbly",
        "Mushroom", "Blooming", "Firefly", "Crowned", "Cowboy", "Smelly"
    };

    public static MutagenicPoolRules Instance { get; } = new();

    private MutagenicPoolRules() { }

    public double GetPityChance(int previousFailures) =>
        Math.Min(MaximumChance - BaseChance,
            PityPerFailure * Math.Clamp(previousFailures, 0, 9_999));

    public double GetContributionChance(int previousFailures, double contribution) =>
        Math.Clamp(double.IsFinite(contribution) ? contribution : 0, 0,
            Math.Max(0, MaximumChance - BaseChance - this.GetPityChance(previousFailures)));

    public double GetChance(int previousFailures, double contribution = 0) =>
        Math.Min(MaximumChance, BaseChance + this.GetPityChance(previousFailures)
            + this.GetContributionChance(previousFailures, contribution));

    public static bool IsApprovedMutation(string? trait) =>
        !string.IsNullOrWhiteSpace(trait) && MutationTraits.Contains(trait, StringComparer.Ordinal);

    public static void NormalizeConfig(ModConfig config)
    {
        config.SlimePrinterFuelCost = Math.Clamp(config.SlimePrinterFuelCost, 1, 999_999);
        config.SlimePrinterDurationDays = Math.Clamp(config.SlimePrinterDurationDays, 1, 28);
        config.TrampolineRecipePrice = Math.Clamp(config.TrampolineRecipePrice, 0, 999_999);
        config.SlimeCushionRecipePrice = Math.Clamp(config.SlimeCushionRecipePrice, 0, 999_999);
        config.SlimeSlideRecipePrice = Math.Clamp(config.SlimeSlideRecipePrice, 0, 999_999);
        config.SlimeSeesawRecipePrice = Math.Clamp(config.SlimeSeesawRecipePrice, 0, 999_999);
        config.HeatedSlimeStoneRecipePrice = Math.Clamp(config.HeatedSlimeStoneRecipePrice, 0, 999_999);
        config.SlimeTunnelRecipePrice = Math.Clamp(config.SlimeTunnelRecipePrice, 0, 999_999);
        config.SlimePondRecipePrice = Math.Clamp(config.SlimePondRecipePrice, 0, 999_999);
        config.SlimeSwingRecipePrice = Math.Clamp(config.SlimeSwingRecipePrice, 0, 999_999);
        config.SlimeLampRecipePrice = Math.Clamp(config.SlimeLampRecipePrice, 0, 999_999);
        config.SlimeFairyLightsRecipePrice = Math.Clamp(config.SlimeFairyLightsRecipePrice, 0, 999_999);
        config.SlimeBallPitRecipePrice = Math.Clamp(config.SlimeBallPitRecipePrice, 0, 999_999);
    }
}
