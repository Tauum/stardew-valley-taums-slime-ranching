using StardewModdingAPI.Events;

namespace taums_slime_ranching.Services;

/// <summary>Registers native crafting recipes for the implemented Daycare furniture.</summary>
internal static class FurnitureRecipeContent
{
    private static readonly Dictionary<string, string> Recipes = new(StringComparer.Ordinal)
    {
        ["Slime Trampoline"] = CreateRecipe("388 50 771 20 766 5", FurnitureContent.TrampolineItemId, "Slime Trampoline"),
        ["Slime Cushion"] = CreateRecipe("771 30 766 5", FurnitureContent.SlimeCushionItemId, "Slime Cushion"),
        ["Slime Slide"] = CreateRecipe("388 50 390 15", FurnitureContent.SlimeSlideItemId, "Slime Slide"),
        ["Slime Seesaw"] = CreateRecipe("388 75 335 2 771 10", FurnitureContent.SlimeSeesawItemId, "Slime Seesaw"),
        ["Heated Slime Stone"] = CreateRecipe("390 50 382 5 766 5", FurnitureContent.HeatedSlimeStoneItemId, "Heated Slime Stone"),
        ["Slime Pond"] = CreateRecipe("390 75 766 20 771 10", FurnitureContent.SlimePondItemId, "Slime Pond"),
        ["Slime Tunnel"] = CreateRecipe("388 60 771 30 766 10", FurnitureContent.SlimeTunnelItemId, "Slime Tunnel"),
        ["Slime Swing"] = CreateRecipe("388 60 771 20 335 2", FurnitureContent.SlimeSwingItemId, "Slime Swing"),
        ["Slime Lamp"] = CreateRecipe("388 30 335 2 382 5", FurnitureContent.SlimeLampItemId, "Slime Lamp"),
        ["Slime Fairy Lights"] = CreateRecipe("771 50 92 20 768 5", FurnitureContent.SlimeFairyLightsItemId, "Slime Fairy Lights"),
        ["Slime Ball Pit"] = CreateRecipe("388 50 771 50 766 20", FurnitureContent.SlimeBallPitItemId, "Slime Ball Pit")
    };

    private static readonly HashSet<string> RecipeNames = new(Recipes.Keys, StringComparer.Ordinal);

    public static bool IsFurnitureRecipe(string name) => RecipeNames.Contains(name);

    public static void EditAsset(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            return;

        e.Edit(asset =>
        {
            var recipes = asset.AsDictionary<string, string>().Data;
            foreach ((string name, string data) in Recipes)
                recipes.TryAdd(name, data);
        });
    }

    private static string CreateRecipe(string ingredients, string outputItemId, string displayName) =>
        $"{ingredients}/Field/{outputItemId} 1/true//{displayName}";
}
