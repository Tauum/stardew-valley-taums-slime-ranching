using taums_slime_ranching.Models;

namespace taums_slime_ranching.Services;

/// <summary>Maps species identity to the ability identifiers companion AI will later execute.</summary>
internal static class SlimeAbilityCatalog
{
    private static readonly IReadOnlyDictionary<string, string> BaseAbilities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Red"] = "Fighter",
        ["Yellow"] = "Revealer",
        ["Blue"] = "Irrigator",
        ["Green"] = "Collector",
        ["Orange"] = "Nurturer",
        ["Purple"] = "Prospector",
        ["Violet"] = "Prospector",
        ["Pink"] = "Healer"
    };

    public static void Refresh(SlimeGenome genome)
    {
        genome.AbilityIds = GetAbilityNames(genome.PrimarySpecies, genome.SecondarySpecies, genome.VisualColor).ToList();
    }

    public static IReadOnlyList<string> GetAbilityNames(string primarySpecies, string? secondarySpecies, string? visualColor)
    {
        var abilities = new List<string>(2);
        AddAbility(primarySpecies, visualColor, abilities);
        if (!string.IsNullOrWhiteSpace(secondarySpecies))
            AddAbility(secondarySpecies, visualColor, abilities);
        return abilities.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static void AddAbility(string species, string? visualColor, List<string> output)
    {
        if (species.StartsWith("Golden", StringComparison.OrdinalIgnoreCase)
            || species.StartsWith("Prismatic", StringComparison.OrdinalIgnoreCase)
            || species.StartsWith("Black", StringComparison.OrdinalIgnoreCase)
            || species.StartsWith("White", StringComparison.OrdinalIgnoreCase))
            return;

        string? baseColor = BaseAbilities.Keys.FirstOrDefault(color => species.StartsWith(color, StringComparison.OrdinalIgnoreCase));
        if (baseColor is null && species.Equals("Ranch Slime", StringComparison.OrdinalIgnoreCase))
            baseColor = FarmAnimalContent.GetClosestVariantName(visualColor);
        if (baseColor is not null && BaseAbilities.TryGetValue(baseColor, out string? ability))
            output.Add(ability);
    }
}
