namespace taums_slime_ranching.Services;

internal static class CompostTroughRules
{
    // Seaweed, Green Algae, and White Algae are useful trough inputs without changing the genome's like-food roll.
    private static readonly HashSet<string> AlgaeItemIds = new(StringComparer.Ordinal)
    {
        "(O)152", "(O)153", "(O)157"
    };

    public static bool IsEligibleFood(string? qualifiedItemId) =>
        FoodLikeCatalog.IsEligible(qualifiedItemId)
        || (qualifiedItemId is not null && AlgaeItemIds.Contains(qualifiedItemId));
}
