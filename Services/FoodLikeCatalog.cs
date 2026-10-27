using StardewValley;

namespace taums_slime_ranching.Services;

/// <summary>Vanilla monster loot and fishing trash eligible as a slime's one favorite food.</summary>
internal static class FoodLikeCatalog
{
    private static readonly string[] ItemIds =
    {
        // Monster loot: Bug Meat, Slime, Bat Wing, Solar Essence, Void Essence.
        "(O)684", "(O)766", "(O)767", "(O)768", "(O)769",
        // Fishing trash: Joja Cola, Trash, Driftwood, Broken Glasses, Broken CD, Soggy Newspaper.
        "(O)167", "(O)168", "(O)169", "(O)170", "(O)171", "(O)172"
    };

    public static bool IsEligible(string? qualifiedItemId) =>
        qualifiedItemId is not null && ItemIds.Contains(qualifiedItemId, StringComparer.Ordinal);

    public static string GetDisplayName(string? qualifiedItemId) => qualifiedItemId switch
    {
        "(O)684" => "Bug Meat",
        "(O)766" => "Slime",
        "(O)767" => "Bat Wing",
        "(O)768" => "Solar Essence",
        "(O)769" => "Void Essence",
        "(O)167" => "Joja Cola",
        "(O)168" => "Trash",
        "(O)169" => "Driftwood",
        "(O)170" => "Broken Glasses",
        "(O)171" => "Broken CD",
        "(O)172" => "Soggy Newspaper",
        _ => "Unknown"
    };

    /// <summary>Assign the same favorite for a given slime ID on every load.</summary>
    public static string ForSlime(Guid slimeId)
    {
        if (slimeId == Guid.Empty)
            throw new ArgumentException("A slime needs an ID before it can receive a food like.", nameof(slimeId));

        return ItemIds[slimeId.ToByteArray()[0] % ItemIds.Length];
    }

    /// <summary>Choose an eligible favorite for randomized test genomes.</summary>
    public static string GetRandomItemId() => ItemIds[Game1.random.Next(ItemIds.Length)];
}
