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
        var abilities = new List<string>(2);
        AddAbility(genome.PrimarySpecies, genome.VisualColor, abilities);
        if (!string.IsNullOrWhiteSpace(genome.SecondarySpecies))
            AddAbility(genome.SecondarySpecies, genome.VisualColor, abilities);
        genome.AbilityIds = abilities.Distinct(StringComparer.Ordinal).ToList();
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
