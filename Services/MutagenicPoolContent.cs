using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;

namespace taums_slime_ranching.Services;

internal static class MutagenicPoolContent
{
    public const string ItemId = "Taum.SlimeRanching_MutagenicPool";
    public const string QualifiedItemId = "(BC)" + ItemId;

    public static void EditAsset(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
            return;

        e.Edit(asset =>
        {
            var craftables = asset.AsDictionary<string, BigCraftableData>().Data;
            craftables.TryAdd(ItemId, new BigCraftableData
            {
                Name = "Mutagenic Pool",
                DisplayName = "Mutagenic Pool",
                Description = "Uses vanilla Slime to try to awaken a mutation in a Domesticated Slime.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = RanchingItemContent.IconTextureName,
                SpriteIndex = 3
            });
        });
    }
}
