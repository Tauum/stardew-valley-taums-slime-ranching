using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;

namespace taums_slime_ranching.Services;

internal static class GuidebookContent
{
    private const string ItemId = "Taum.SlimeRanching_FieldGuide";
    public const string QualifiedItemId = "(O)" + ItemId;

    // Keep the short in-game introduction readable; the linked markdown guide has the full instructions.
    public const string Text = "Taum's Slime Ranching Field Guide^\n\nThe Slime Incubator inside the Slime Daycare accepts genome-specific Ranch Slime Eggs. Place one egg in the incubator to hatch the recorded slime; its identity and traits are not rerolled. Each egg carries its own genome metadata and cannot stack with another Ranch Slime Egg. The story's starter egg and its one-time split are handled through the Daycare introduction.\n\nThe Gel Refinery currently recycles Broken Glasses and vanilla Slime into Refined Quartz. The fuel and processing time are configurable; the recipe is provisional. The Compost Trough is a retired prototype; old placed instances are inert and kept only for save compatibility.\n\nTo test machines, run their slime_ranching_give_* command followed by your exact save folder name in the SMAPI console. Facility jobs are stored in the current save, Ranch Slime genomes on their animal entities, and egg genomes on the eggs. Modded Ranch Slimes live in the separate Slime Daycare; vanilla GreenSlime monsters and vanilla Slime Eggs remain unchanged. Use slime_ranching_inspect inside the Daycare or vanilla Slime Hutch to check current counts and jobs. See the mod's Guides/PLAYER_GUIDE.md for full instructions and current feature status.";

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
