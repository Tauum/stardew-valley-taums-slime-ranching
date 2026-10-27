using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;

namespace taums_slime_ranching.Services;

internal static class GuidebookContent
{
    private const string ItemId = "Taum.SlimeRanching_FieldGuide";
    public const string QualifiedItemId = "(O)" + ItemId;

    // Keep the short in-game introduction readable; the linked markdown guide has the full instructions.
    public const string Text = "Taum's Slime Ranching Field Guide^\n\nThe Slime Egg Converter accepts one green, blue, red, purple, or tiger Slime Egg. Put the egg into the converter and collect one Common Slime Egg after ten in-game minutes. Vanilla eggs elsewhere are unchanged.\n\nCommon Slime Eggs and genome-specific Ranch Slime Eggs hatch in the custom Slime Incubator inside the Slime Daycare. A Common Egg receives a fresh base Common genome when inserted; a breeder egg keeps its stored genome. The egg hatches after two days when the Daycare has room.\n\nThe Gel Refinery currently recycles one Broken Glasses and vanilla Slime into Refined Quartz. The fuel and processing time are configurable; the recipe is provisional.\n\nTo test machines, run their slime_ranching_give_* command followed by your exact save folder name in the SMAPI console. Genome details and machine jobs are stored in the mod folder. Modded Ranch Slimes live in the separate Slime Daycare; vanilla GreenSlime monsters remain unchanged. Use slime_ranching_inspect inside the Daycare or vanilla Slime Hutch to check current counts and jobs. See the mod's Guides/PLAYER_GUIDE.md for full instructions and current feature status.";

    public static void EditAsset(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            return;

        e.Edit(asset =>
        {
            var objects = asset.AsDictionary<string, ObjectData>().Data;
            if (!objects.ContainsKey(ItemId))
            {
                objects[ItemId] = new ObjectData
                {
                    Name = "Slime Ranching Field Guide",
                    DisplayName = "Slime Ranching Field Guide",
                    Description = "A practical guide to raising and studying slimes.",
                    Type = "Basic",
                    Price = 0,
                    Texture = "Maps/springobjects",
                    SpriteIndex = 842,
                    CanBeGivenAsGift = false,
                    CanBeTrashed = true,
                    ExcludeFromShippingCollection = true,
                    ContextTags = new List<string> { "taum_slime_ranching_guide" }
                };
            }
        });
    }
}
