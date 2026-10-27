namespace taums_slime_ranching.Services;

/// <summary>Validated, per-session tuning values for the Mutagenic Pool.</summary>
internal sealed class MutagenicPoolRules
{
    private MutagenicPoolRules(int slimeCost, double baseChance, double pityIncrease, double maximumChance, int durationDays)
    {
        this.SlimeCost = slimeCost;
        this.BaseChance = baseChance;
        this.PityIncrease = pityIncrease;
        this.MaximumChance = maximumChance;
        this.DurationDays = durationDays;
    }

    public int SlimeCost { get; }
    public double BaseChance { get; }
    public double PityIncrease { get; }
    public double MaximumChance { get; }
    public int DurationDays { get; }

    public static MutagenicPoolRules FromConfig(ModConfig config)
    {
        double baseChance = Math.Clamp(config.MutagenicPoolBaseChance, 0, 1);
        return new MutagenicPoolRules(
            Math.Clamp(config.MutagenicPoolSlimeCost, 1, 999_999),
            baseChance,
            Math.Clamp(config.MutagenicPoolChancePerFailure, 0, 1),
            Math.Clamp(config.MutagenicPoolMaximumChance, baseChance, 1),
            Math.Clamp(config.MutagenicPoolDurationDays, 1, 28));
    }

    public static void NormalizeConfig(ModConfig config)
    {
        BuildingContent.NormalizeConfig(config);
        MutagenicPoolRules rules = FromConfig(config);
        config.MutagenicPoolSlimeCost = rules.SlimeCost;
        config.MutagenicPoolBaseChance = rules.BaseChance;
        config.MutagenicPoolChancePerFailure = rules.PityIncrease;
        config.MutagenicPoolMaximumChance = rules.MaximumChance;
        config.MutagenicPoolDurationDays = rules.DurationDays;
        config.GelRefineryShopPrice = Math.Clamp(config.GelRefineryShopPrice, 0, 999_999);
        config.GelRefinerySlimeFuelCost = Math.Clamp(config.GelRefinerySlimeFuelCost, 1, 999_999);
        config.GelRefineryDurationDays = Math.Clamp(config.GelRefineryDurationDays, 1, 28);
        config.CompostTroughShopPrice = Math.Clamp(config.CompostTroughShopPrice, 0, 999_999);
        config.CompostTroughGrowthDays = Math.Clamp(config.CompostTroughGrowthDays, 1, 3);
        config.CompostTroughLikedFoodBonusDays = Math.Clamp(config.CompostTroughLikedFoodBonusDays, 0, 2);
        config.SlimeTunnelShopPrice = Math.Clamp(config.SlimeTunnelShopPrice, 0, 999_999);
        config.HeatedSlimeStoneShopPrice = Math.Clamp(config.HeatedSlimeStoneShopPrice, 0, 999_999);
        config.SlimePondShopPrice = Math.Clamp(config.SlimePondShopPrice, 0, 999_999);
        config.SlimeSwingShopPrice = Math.Clamp(config.SlimeSwingShopPrice, 0, 999_999);
        config.SlimeFairyLightsShopPrice = Math.Clamp(config.SlimeFairyLightsShopPrice, 0, 999_999);
        config.ConverterShopPrice = Math.Clamp(config.ConverterShopPrice, 0, 999_999);
        config.BreederShopPrice = Math.Clamp(config.BreederShopPrice, 0, 999_999);
        config.IncubatorShopPrice = Math.Clamp(config.IncubatorShopPrice, 0, 999_999);
        config.TrampolineShopPrice = Math.Clamp(config.TrampolineShopPrice, 0, 999_999);
        config.SlimeCushionShopPrice = Math.Clamp(config.SlimeCushionShopPrice, 0, 999_999);
        config.SlimePlaygroundShopPrice = Math.Clamp(config.SlimePlaygroundShopPrice, 0, 999_999);
        config.SlimeSlideShopPrice = Math.Clamp(config.SlimeSlideShopPrice, 0, 999_999);
        config.SlimeBedShopPrice = Math.Clamp(config.SlimeBedShopPrice, 0, 999_999);
        config.MutagenicPoolShopPrice = Math.Clamp(config.MutagenicPoolShopPrice, 0, 999_999);
    }

    public double GetChance(int previousFailures) =>
        Math.Min(this.MaximumChance, this.BaseChance + this.PityIncrease * Math.Clamp(previousFailures, 0, 9_999));
}
