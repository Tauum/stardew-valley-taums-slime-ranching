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
        config.GelRefinerySlimeFuelCost = Math.Clamp(config.GelRefinerySlimeFuelCost, 1, 999_999);
        config.GelRefineryDurationDays = Math.Clamp(config.GelRefineryDurationDays, 1, 28);
        config.SlimeTunnelShopPrice = Math.Clamp(config.SlimeTunnelShopPrice, 0, 999_999);
        config.HeatedSlimeStoneShopPrice = Math.Clamp(config.HeatedSlimeStoneShopPrice, 0, 999_999);
        config.SlimePondShopPrice = Math.Clamp(config.SlimePondShopPrice, 0, 999_999);
        config.SlimeSwingShopPrice = Math.Clamp(config.SlimeSwingShopPrice, 0, 999_999);
        config.SlimeFairyLightsShopPrice = Math.Clamp(config.SlimeFairyLightsShopPrice, 0, 999_999);
        config.TrampolineShopPrice = Math.Clamp(config.TrampolineShopPrice, 0, 999_999);
        config.SlimeCushionShopPrice = Math.Clamp(config.SlimeCushionShopPrice, 0, 999_999);
        config.SlimeSlideShopPrice = Math.Clamp(config.SlimeSlideShopPrice, 0, 999_999);
    }
}
