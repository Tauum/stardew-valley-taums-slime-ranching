using Microsoft.Xna.Framework;
using taums_slime_ranching.Models;

namespace taums_slime_ranching.Services;

internal static class BreedingRules
{
    public const int BasicCycleDurationDays = 18;
    public const int FirstUpgradeCycleDurationDays = 14;

    private static readonly string[] BaseSpecies = { "Red", "Orange", "Yellow", "Green", "Blue", "Purple", "Pink" };
    private static readonly string[] ExceptionalSpecies = { "Gold", "Golden", "Void", "Prismatic", "Black", "White" };

    internal readonly record struct Offspring(string PrimarySpecies, string? SecondarySpecies, string VisualColor);

    public static bool IsBreedable(SlimeGenome genome)
    {
        if (IsExceptional(genome.PrimarySpecies) || IsExceptional(genome.SecondarySpecies))
            return false;

        return GetContributors(genome) is not null;
    }

    public static IReadOnlyList<Offspring> GetPossibleOffspring(SlimeGenome firstParent, SlimeGenome secondParent)
    {
        string[]? first = GetContributors(firstParent);
        string[]? second = GetContributors(secondParent);
        if (first is null || second is null
            || IsExceptional(firstParent.PrimarySpecies) || IsExceptional(firstParent.SecondarySpecies)
            || IsExceptional(secondParent.PrimarySpecies) || IsExceptional(secondParent.SecondarySpecies))
            return Array.Empty<Offspring>();

        if (first.Length == 1 && second.Length == 1)
        {
            if (first[0] == second[0])
                return new[] { CreateBase(first[0]) };

            return new[] { CreateHybrid(first[0], second[0]) };
        }

        if (first.Length == 2 && second.Length == 2)
        {
            if (SameHybrid(first, second))
                return new[] { CreateHybrid(first[0], first[1]) };

            // Keep all four slots, even when the same base species appears twice.
            return new[]
            {
                CreateBase(first[0]), CreateBase(first[1]),
                CreateBase(second[0]), CreateBase(second[1])
            };
        }

        string[] hybrid = first.Length == 2 ? first : second;
        string baseSpecies = first.Length == 1 ? first[0] : second[0];
        return new[] { CreateBase(baseSpecies), CreateHybrid(hybrid[0], hybrid[1]) };
    }

    public static string Describe(Offspring offspring)
    {
        string primary = GetDisplaySpecies(offspring.PrimarySpecies);
        return offspring.SecondarySpecies is null
            ? primary
            : $"{primary}/{GetDisplaySpecies(offspring.SecondarySpecies)} hybrid";
    }

    private static string[]? GetContributors(SlimeGenome genome)
    {
        if (genome.SecondarySpecies is null)
        {
            string? baseSpecies = ParseBaseSpecies(genome.PrimarySpecies)
                ?? FarmAnimalContent.GetClosestVariantName(genome.VisualColor);
            return new[] { baseSpecies };
        }

        string? first = ParseBaseSpecies(genome.PrimarySpecies);
        string? second = ParseBaseSpecies(genome.SecondarySpecies);
        if (first is null || second is null || first == second)
            return null;

        (first, second) = SortHybrid(first, second);
        return new[] { first, second };
    }

    private static string? ParseBaseSpecies(string? species)
    {
        if (string.IsNullOrWhiteSpace(species))
            return null;

        string? color = BaseSpecies.FirstOrDefault(candidate => species.StartsWith(candidate, StringComparison.OrdinalIgnoreCase));
        if (color is not null)
            return color;
        return species.StartsWith("Violet", StringComparison.OrdinalIgnoreCase) ? "Purple" : null;
    }

    private static bool IsExceptional(string? species) => !string.IsNullOrWhiteSpace(species)
        && ExceptionalSpecies.Any(candidate => species.StartsWith(candidate, StringComparison.OrdinalIgnoreCase));

    private static bool SameHybrid(string[] first, string[] second) =>
        first.Length == 2 && second.Length == 2
        && first[0] == second[0] && first[1] == second[1];

    private static (string First, string Second) SortHybrid(string first, string second)
    {
        int firstIndex = Array.IndexOf(BaseSpecies, first);
        int secondIndex = Array.IndexOf(BaseSpecies, second);
        return firstIndex <= secondIndex ? (first, second) : (second, first);
    }

    private static Offspring CreateBase(string color) =>
        new($"{color} Slime", null, FarmAnimalContent.GetReferenceHex(color));

    private static Offspring CreateHybrid(string first, string second)
    {
        (first, second) = SortHybrid(first, second);
        Color firstColor = ParseGenomeColor(FarmAnimalContent.GetReferenceHex(first));
        Color secondColor = ParseGenomeColor(FarmAnimalContent.GetReferenceHex(second));
        string blended = $"#{(firstColor.R + secondColor.R) / 2:X2}{(firstColor.G + secondColor.G) / 2:X2}{(firstColor.B + secondColor.B) / 2:X2}";
        return new Offspring($"{first} Slime", $"{second} Slime", blended);
    }

    private static string GetDisplaySpecies(string species) =>
        species.EndsWith(" Slime", StringComparison.OrdinalIgnoreCase) ? species[..^6] : species;

    private static Color ParseGenomeColor(string hex) => new(
        Convert.ToByte(hex.Substring(1, 2), 16),
        Convert.ToByte(hex.Substring(3, 2), 16),
        Convert.ToByte(hex.Substring(5, 2), 16));
}
