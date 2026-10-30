using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;

namespace taums_slime_ranching.Services;

internal static class GuidebookContent
{
    private const string ItemId = "Taum.SlimeRanching_FieldGuide";
    public const string QualifiedItemId = "(O)" + ItemId;

    // Keep the short in-game introduction readable; the linked markdown guide has the full instructions.
    public const string Text = "Taum's Slime Ranching Field Guide^\n\nThe Slime Incubator inside the Slime Daycare accepts genome-specific Domesticated Slime Eggs. The seven-day incubation uses the egg's stored genome, and the egg can hatch only once. After hatching, each Domesticated Slime takes seven in-game days to mature; that growth period follows incubation. Each egg carries unique metadata and cannot stack with another Domesticated Slime Egg. While an egg incubates and its Rare Lab is unlocked, contribute 10 vanilla Slime for +1 percentage point. In an empty unlocked Rare Lab, choose a catalyst from your inventory: 10 Gold Bars target Gold Slime, 1 Void Egg or 20 Void Essence target Void Slime, and 1 Prismatic Shard targets Prismatic Slime. A catalyst remains stored after a failed rare roll and is consumed by a successful roll. Without a catalyst, rare results currently select Gold, Void, or Prismatic Slime with equal odds, independently of the egg's species. A full Daycare keeps the fixed result waiting until space is available. The lab's pity and contribution then follow its success or failure rule.\n\nTo test Egg CryoLab storage on an upgraded Daycare, run slime_ranching_give_cryolab in the SMAPI console while a save is loaded, then place the tester chest inside. Tester commands warn and stop if no save is loaded. It accepts unused Domesticated Slime Eggs only and does not start incubation; the fixed facility placement is still being tested. The Egg CryoLab remains in development.\n\nThe Slime Printer currently recycles Broken Glasses and vanilla Slime into Refined Quartz. New batches unlock at the final Daycare tier. Fuel and processing time are configurable, with a 10-Slime minimum fuel cost to prevent direct sale-value profit. The recipe remains provisional. The Compost Trough is a retired prototype; old placed instances are inert and kept only for save compatibility.\n\nTester commands automatically use the loaded save and warn and stop if no save is loaded. To test machines, run their slime_ranching_give_* command in the SMAPI console. Facility jobs are stored in the current save, Domesticated Slime genomes on their animal entities, and egg genomes on the eggs. Modded Domesticated Slimes live in the separate Slime Daycare; vanilla GreenSlime monsters and vanilla Slime Eggs remain unchanged. Use slime_ranching_inspect inside the Daycare or vanilla Slime Hutch to check current counts and jobs. See the mod's Guides/PLAYER_GUIDE.md for full instructions and current feature status.";

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
