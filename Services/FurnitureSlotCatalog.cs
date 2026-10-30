namespace taums_slime_ranching.Services;

internal sealed record DaycareFurnitureSlotDefinition(string SlotId, int RequiredBuildingTier);

internal sealed record DaycareFurnitureRecipeListing(
    string ShopItemName,
    string QualifiedItemId,
    int RequiredBuildingTier,
    Func<ModConfig, int> GetPrice);

/// <summary>Stable slot identities, tier progression, and recipe unlocks for Daycare furniture.</summary>
internal static class FurnitureSlotCatalog
{
    private static readonly IReadOnlyList<DaycareFurnitureSlotDefinition> Slots = Array.AsReadOnly(
        Enumerable.Range(1, 15)
            .Select(index => new DaycareFurnitureSlotDefinition(
                $"slot-{index:00}",
                index <= 3 ? 0 : index <= 7 ? 1 : 3))
            .ToArray());

    private static readonly IReadOnlyList<DaycareFurnitureRecipeListing> Recipes = Array.AsReadOnly(new[]
    {
        new DaycareFurnitureRecipeListing("Trampoline", FurnitureContent.TrampolineQualifiedId, 0, config => config.TrampolineRecipePrice),
        new DaycareFurnitureRecipeListing("SlimeCushion", FurnitureContent.SlimeCushionQualifiedId, 0, config => config.SlimeCushionRecipePrice),
        new DaycareFurnitureRecipeListing("SlimeSlide", FurnitureContent.SlimeSlideQualifiedId, 0, config => config.SlimeSlideRecipePrice),
        new DaycareFurnitureRecipeListing("SlimeSeesaw", FurnitureContent.SlimeSeesawQualifiedId, 0, config => config.SlimeSeesawRecipePrice),
        new DaycareFurnitureRecipeListing("HeatedSlimeStone", FurnitureContent.HeatedSlimeStoneQualifiedId, 0, config => config.HeatedSlimeStoneRecipePrice),
        new DaycareFurnitureRecipeListing("SlimeTunnel", FurnitureContent.SlimeTunnelQualifiedId, 1, config => config.SlimeTunnelRecipePrice),
        new DaycareFurnitureRecipeListing("SlimePond", FurnitureContent.SlimePondQualifiedId, 1, config => config.SlimePondRecipePrice),
        new DaycareFurnitureRecipeListing("SlimeSwing", FurnitureContent.SlimeSwingQualifiedId, 1, config => config.SlimeSwingRecipePrice),
        new DaycareFurnitureRecipeListing("SlimeLamp", FurnitureContent.SlimeLampQualifiedId, 1, config => config.SlimeLampRecipePrice),
        new DaycareFurnitureRecipeListing("SlimeFairyLights", FurnitureContent.SlimeFairyLightsQualifiedId, 3, config => config.SlimeFairyLightsRecipePrice),
        new DaycareFurnitureRecipeListing("SlimeBallPit", FurnitureContent.SlimeBallPitQualifiedId, 3, config => config.SlimeBallPitRecipePrice),
        new DaycareFurnitureRecipeListing("SlimeWashingMachine", FurnitureContent.SlimeWashingMachineQualifiedId, 3, config => config.SlimeWashingMachineRecipePrice)
    });
    private static readonly IReadOnlyDictionary<string, DaycareFurnitureRecipeListing> RecipesByItemId =
        Recipes.ToDictionary(recipe => recipe.QualifiedItemId, StringComparer.Ordinal);

    public static IReadOnlyList<DaycareFurnitureSlotDefinition> GetSlotsUnlockedAtTier(int buildingTier) =>
        Slots.Where(slot => slot.RequiredBuildingTier <= buildingTier).ToArray();

    public static int GetSlotCountAtTier(int buildingTier) =>
        Slots.Count(slot => slot.RequiredBuildingTier <= buildingTier);

    public static bool TryGetSlot(string slotId, out DaycareFurnitureSlotDefinition slot)
    {
        slot = Slots.FirstOrDefault(entry => entry.SlotId == slotId)!;
        return slot is not null;
    }

    public static IReadOnlyList<DaycareFurnitureRecipeListing> GetRecipeListings() => Recipes;

    public static bool TryGetFurniture(string qualifiedItemId, out DaycareFurnitureRecipeListing recipe) =>
        RecipesByItemId.TryGetValue(qualifiedItemId, out recipe!);

    public static bool TryGetFurnitureByCommandKey(string commandKey, out DaycareFurnitureRecipeListing recipe)
    {
        recipe = Recipes.FirstOrDefault(entry => string.Equals(GetCommandKey(entry), commandKey, StringComparison.OrdinalIgnoreCase))!;
        return recipe is not null;
    }

    public static string GetCommandKey(DaycareFurnitureRecipeListing recipe) =>
        recipe.ShopItemName.StartsWith("Slime", StringComparison.Ordinal)
            ? recipe.ShopItemName["Slime".Length..]
            : recipe.ShopItemName;
}
