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
        MutagenicPoolRules rules = FromConfig(config);
        config.MutagenicPoolSlimeCost = rules.SlimeCost;
        config.MutagenicPoolBaseChance = rules.BaseChance;
        config.MutagenicPoolChancePerFailure = rules.PityIncrease;
        config.MutagenicPoolMaximumChance = rules.MaximumChance;
        config.MutagenicPoolDurationDays = rules.DurationDays;
        config.MutagenicPoolShopPrice = Math.Clamp(config.MutagenicPoolShopPrice, 0, 999_999);
    }

    public double GetChance(int previousFailures) =>
        Math.Min(this.MaximumChance, this.BaseChance + this.PityIncrease * Math.Clamp(previousFailures, 0, 9_999));
}
