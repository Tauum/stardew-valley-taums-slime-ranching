using taums_slime_ranching.Models;

namespace taums_slime_ranching.Services;

internal static class RareLabRules
{
    // Black and White remain deferred; the current active no-catalyst selection uses the approved core roster.
    private static readonly string[] ActiveRareOffspringSpecies = { "Gold Slime", "Void Slime", "Prismatic Slime" };

    public const int FirstTierBaseChancePoints = 1;
    public const int FinalTierBaseChancePoints = 5;
    public const int SlimeUnitsPerContributionPoint = 10;
    public const string GoldBarQualifiedItemId = "(O)336";
    public const int GoldBarsPerCatalyst = 10;
    public const string GoldCatalystSpecies = "Gold Slime";
    public const int MaximumChancePoints = 100;

    public static IReadOnlyList<string> RareOffspringSpecies => ActiveRareOffspringSpecies;

    public static bool IsRareOffspringSpecies(string? species) =>
        !string.IsNullOrWhiteSpace(species)
        && ActiveRareOffspringSpecies.Contains(species, StringComparer.OrdinalIgnoreCase);

    public static int GetChancePoints(RareLabRecord? lab) => lab is { IsUnlocked: true }
        ? Math.Clamp(lab.BaseChancePoints + lab.PityPoints + lab.ContributionPoints, 0, MaximumChancePoints)
        : 0;

    public static bool IsValid(RareLabRecord lab) =>
        !string.IsNullOrWhiteSpace(lab.MachineKey)
        && lab.PityPoints is >= 0 and <= MaximumChancePoints
        && lab.ContributionPoints is >= 0 and <= MaximumChancePoints
        && (lab.CatalystSpecies is null || (lab.IsUnlocked && lab.CatalystSpecies == GoldCatalystSpecies))
        && (lab.IsUnlocked
            ? lab.BaseChancePoints is FirstTierBaseChancePoints or FinalTierBaseChancePoints
            : lab.BaseChancePoints == 0 && lab.PityPoints == 0 && lab.ContributionPoints == 0);

    public static bool AddContribution(RareLabRecord lab)
    {
        if (!lab.IsUnlocked || GetChancePoints(lab) >= MaximumChancePoints)
            return false;

        lab.ContributionPoints++;
        return true;
    }

    public static void SetFinalTier(RareLabRecord lab)
    {
        if (lab.IsUnlocked)
            lab.BaseChancePoints = FinalTierBaseChancePoints;
    }

    public static void ApplyOutcome(RareLabRecord lab, bool rareOffspringProduced)
    {
        if (!lab.IsUnlocked)
            return;

        if (rareOffspringProduced)
        {
            lab.ContributionPoints = 0;
            lab.CatalystSpecies = null;
            return;
        }

        lab.PityPoints = Math.Min(MaximumChancePoints, lab.PityPoints + 1);
        if (lab.ContributionPoints > 2)
            lab.ContributionPoints--;
    }
}
