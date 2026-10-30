using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;

namespace taums_slime_ranching.Services;

internal static class SlimePrinterContent
{
    public const string ItemId = "Taum.SlimeRanching_SlimePrinter";
    public const string QualifiedItemId = "(BC)" + ItemId;

    public static bool IsSlimePrinter(StardewValley.Object obj) =>
        obj.QualifiedItemId == QualifiedItemId;

    public static void EditAsset(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
            return;

        e.Edit(asset =>
        {
            var craftables = asset.AsDictionary<string, BigCraftableData>().Data;
            craftables.TryAdd(ItemId, new BigCraftableData
            {
                Name = "Slime Printer",
                DisplayName = "Slime Printer",
                Description = "Recycle discarded items with vanilla Slime fuel.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = RanchingItemContent.IconTextureName,
                SpriteIndex = 4
            });
        });
    }
}
