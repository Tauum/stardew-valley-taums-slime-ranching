using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;

namespace taums_slime_ranching.Services;

/// <summary>Defines the physical Rare Lab fixture; its research state remains linked to an incubator.</summary>
internal static class ResearchLabContent
{
    public const string ItemId = "Taum.SlimeRanching_ResearchLab";
    public const string QualifiedItemId = "(BC)" + ItemId;

    public static bool IsResearchLab(StardewValley.Object obj) => obj.QualifiedItemId == QualifiedItemId;

    public static void EditAsset(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
            return;

        e.Edit(asset =>
        {
            var craftables = asset.AsDictionary<string, BigCraftableData>().Data;
            craftables.TryAdd(ItemId, new BigCraftableData
            {
                Name = "Research Lab",
                DisplayName = "Research Lab",
                Description = "Studies rare offspring for an adjacent Slime Incubator.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = RanchingItemContent.IconTextureName,
                SpriteIndex = 2
            });
        });
    }

    public static bool TryGetAdjacentIncubator(GameLocation location, Vector2 labAnchor, out Vector2 incubatorAnchor)
    {
        MachineFootprint labFootprint = new(4, 3);
        MachineFootprint incubatorFootprint = new(2, 3);
        Vector2? match = null;
        foreach (KeyValuePair<Vector2, StardewValley.Object> pair in location.Objects.Pairs)
        {
            if (pair.Value.QualifiedItemId != RanchingItemContent.IncubatorQualifiedId
                || !MachineArtContent.AreAdjacent(labAnchor, labFootprint, pair.Key, incubatorFootprint))
                continue;

            if (match.HasValue)
            {
                incubatorAnchor = Vector2.Zero;
                return false;
            }

            match = pair.Key;
        }

        incubatorAnchor = match ?? Vector2.Zero;
        return match.HasValue;
    }
}
