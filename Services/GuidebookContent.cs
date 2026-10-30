using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;

namespace taums_slime_ranching.Services;

internal static class GuidebookContent
{
    private const string ItemId = "Taum.SlimeRanching_FieldGuide";
    public const string QualifiedItemId = "(O)" + ItemId;

    // Keep the short in-game introduction readable; the linked markdown guide has the full instructions.
    public const string Text = "Taum's Slime Ranching Field Guide^\n\nThe Slime Incubator inside the Slime Daycare accepts genome-specific Domesticated Slime Eggs. The seven-day incubation uses the egg's stored genome, and the egg can hatch only once. After hatching, each Domesticated Slime takes seven in-game days to mature; that growth period follows incubation. Each egg carries unique metadata and cannot stack with another Domesticated Slime Egg. While an egg incubates and its Rare Lab is unlocked, contribute 10 vanilla Slime for +1 percentage point. You can also load 10 Gold Bars into an empty Rare Lab catalyst slot; a failed rare roll leaves the Gold catalyst stored, while a successful rare roll uses it to produce Gold Slime. Without a catalyst, rare results currently select Gold, Void, or Prismatic Slime with equal odds, independently of the egg's species. A full Daycare keeps the fixed result waiting until space is available. The lab's pity and contribution then follow its success or failure rule.\n\nTo test Egg CryoLab storage on an upgraded Daycare, run slime_ranching_give_cryolab followed by your exact save folder name in the SMAPI console, then place the tester chest inside. It accepts unused Domesticated Slime Eggs only and does not start incubation; the fixed facility placement is still being tested. Other catalyst recipes are in development.\n\nThe Gel Refinery currently recycles Broken Glasses and vanilla Slime into Refined Quartz. New batches unlock at the final Daycare tier. The fuel and processing time are configurable; the recipe is provisional. The Compost Trough is a retired prototype; old placed instances are inert and kept only for save compatibility.\n\nTo test machines, run their slime_ranching_give_* command followed by your exact save folder name in the SMAPI console. Facility jobs are stored in the current save, Domesticated Slime genomes on their animal entities, and egg genomes on the eggs. Modded Domesticated Slimes live in the separate Slime Daycare; vanilla GreenSlime monsters and vanilla Slime Eggs remain unchanged. Use slime_ranching_inspect inside the Daycare or vanilla Slime Hutch to check current counts and jobs. See the mod's Guides/PLAYER_GUIDE.md for full instructions and current feature status.";

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
