using taums_slime_ranching.Models;

namespace taums_slime_ranching.Services;

internal static class RareLabRules
{
    public const int FirstTierBaseChancePoints = 1;
    public const int FinalTierBaseChancePoints = 5;
    public const int SlimeUnitsPerContributionPoint = 10;
    public const int MaximumChancePoints = 100;

    public static int GetChancePoints(RareLabRecord? lab) => lab is { IsUnlocked: true }
        ? Math.Clamp(lab.BaseChancePoints + lab.PityPoints + lab.ContributionPoints, 0, MaximumChancePoints)
        : 0;

    public static bool IsValid(RareLabRecord lab) =>
        !string.IsNullOrWhiteSpace(lab.MachineKey)
        && lab.PityPoints is >= 0 and <= MaximumChancePoints
        && lab.ContributionPoints is >= 0 and <= MaximumChancePoints
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
            return;
        }

        lab.PityPoints = Math.Min(MaximumChancePoints, lab.PityPoints + 1);
        if (lab.ContributionPoints > 2)
            lab.ContributionPoints--;
    }
}
