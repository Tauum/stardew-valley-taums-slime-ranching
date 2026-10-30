using StardewModdingAPI.Events;

namespace taums_slime_ranching.Services;

/// <summary>Registers native crafting recipes and their shared player-facing material list.</summary>
internal static class FurnitureRecipeContent
{
    private sealed record Ingredient(string ItemId, int Amount);

    private sealed record Recipe(string Name, string OutputItemId, string DisplayName, Ingredient[] Ingredients)
    {
        public string GetAssetData() =>
            $"{string.Join(" ", this.Ingredients.Select(ingredient => $"{ingredient.ItemId} {ingredient.Amount}"))}/Field/{this.OutputItemId} 1/true//{this.DisplayName}";

        public string GetIngredientSummary() =>
            $"{this.DisplayName} — {string.Join(", ", this.Ingredients.Select(FurnitureRecipeContent.FormatIngredient))}";
    }

    private static readonly IReadOnlyDictionary<string, (string Singular, string Plural)> IngredientNames = new Dictionary<string, (string Singular, string Plural)>(StringComparer.Ordinal)
    {
        ["388"] = ("Wood", "Wood"),
        ["771"] = ("Fiber", "Fiber"),
        ["766"] = ("vanilla Slime", "vanilla Slime"),
        ["390"] = ("Stone", "Stone"),
        ["335"] = ("Iron Bar", "Iron Bars"),
        ["382"] = ("Coal", "Coal"),
        ["92"] = ("Sap", "Sap"),
        ["768"] = ("Solar Essence", "Solar Essence")
    };

    private static readonly Recipe[] Recipes =
    {
        new("Slime Trampoline", FurnitureContent.TrampolineItemId, "Slime Trampoline", new[] { new Ingredient("388", 50), new Ingredient("771", 20), new Ingredient("766", 5) }),
        new("Slime Cushion", FurnitureContent.SlimeCushionItemId, "Slime Cushion", new[] { new Ingredient("771", 30), new Ingredient("766", 5) }),
        new("Slime Slide", FurnitureContent.SlimeSlideItemId, "Slime Slide", new[] { new Ingredient("388", 50), new Ingredient("390", 15) }),
        new("Slime Seesaw", FurnitureContent.SlimeSeesawItemId, "Slime Seesaw", new[] { new Ingredient("388", 75), new Ingredient("335", 2), new Ingredient("771", 10) }),
        new("Heated Slime Stone", FurnitureContent.HeatedSlimeStoneItemId, "Heated Slime Stone", new[] { new Ingredient("390", 50), new Ingredient("382", 5), new Ingredient("766", 5) }),
        new("Slime Pond", FurnitureContent.SlimePondItemId, "Slime Pond", new[] { new Ingredient("390", 75), new Ingredient("766", 20), new Ingredient("771", 10) }),
        new("Slime Tunnel", FurnitureContent.SlimeTunnelItemId, "Slime Tunnel", new[] { new Ingredient("388", 60), new Ingredient("771", 30), new Ingredient("766", 10) }),
        new("Slime Swing", FurnitureContent.SlimeSwingItemId, "Slime Swing", new[] { new Ingredient("388", 60), new Ingredient("771", 20), new Ingredient("335", 2) }),
        new("Slime Lamp", FurnitureContent.SlimeLampItemId, "Slime Lamp", new[] { new Ingredient("388", 30), new Ingredient("335", 2), new Ingredient("382", 5) }),
        new("Slime Fairy Lights", FurnitureContent.SlimeFairyLightsItemId, "Slime Fairy Lights", new[] { new Ingredient("771", 50), new Ingredient("92", 20), new Ingredient("768", 5) }),
        new("Slime Ball Pit", FurnitureContent.SlimeBallPitItemId, "Slime Ball Pit", new[] { new Ingredient("388", 50), new Ingredient("771", 50), new Ingredient("766", 20) }),
        new("Slime Washing Machine", FurnitureContent.SlimeWashingMachineItemId, "Slime Washing Machine", new[] { new Ingredient("388", 100), new Ingredient("335", 10), new Ingredient("766", 20) })
    };

    private static readonly HashSet<string> RecipeNames = new(Recipes.Select(recipe => recipe.Name), StringComparer.Ordinal);

    public static bool IsFurnitureRecipe(string name) => RecipeNames.Contains(name);

    public static string GetIngredientSummary() => string.Join("; ", Recipes.Select(recipe => recipe.GetIngredientSummary()));

    public static void EditAsset(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            return;

        e.Edit(asset =>
        {
            var recipes = asset.AsDictionary<string, string>().Data;
            foreach (Recipe recipe in Recipes)
                recipes.TryAdd(recipe.Name, recipe.GetAssetData());
        });
    }

    private static string FormatIngredient(Ingredient ingredient)
    {
        (string singular, string plural) = IngredientNames[ingredient.ItemId];
        return $"{ingredient.Amount} {(ingredient.Amount == 1 ? singular : plural)}";
    }
}
