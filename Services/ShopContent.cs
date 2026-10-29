using StardewModdingAPI.Events;
using StardewValley.GameData.Shops;

namespace taums_slime_ranching.Services;

/// <summary>Adds reusable Daycare furniture recipes to Marnie's existing shop.</summary>
internal static class ShopContent
{
    private const string MarnieShopId = "AnimalShop";

    public static void EditAsset(AssetRequestedEventArgs e, ModConfig config)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
            return;

        e.Edit(asset =>
        {
            var shops = asset.AsDictionary<string, ShopData>().Data;
            if (!shops.TryGetValue(MarnieShopId, out ShopData? shop))
                return;

            shop.Items ??= new List<ShopItemData>();
            string anyDaycareCondition = GetDaycareTierCondition(
                BuildingContent.SlimeDaycareBuildingId,
                BuildingContent.SlimeDaycareUpgrade1Id,
                BuildingContent.SlimeDaycareUpgrade2Id,
                BuildingContent.SlimeDaycareUpgrade3Id);
            AddIfMissing(shop, "Trampoline", FurnitureContent.TrampolineQualifiedId, config.TrampolineRecipePrice, anyDaycareCondition);
            AddIfMissing(shop, "SlimeCushion", FurnitureContent.SlimeCushionQualifiedId, config.SlimeCushionRecipePrice, anyDaycareCondition);
            AddIfMissing(shop, "SlimeSlide", FurnitureContent.SlimeSlideQualifiedId, config.SlimeSlideRecipePrice, anyDaycareCondition);
            AddIfMissing(shop, "SlimeSeesaw", FurnitureContent.SlimeSeesawQualifiedId, config.SlimeSeesawRecipePrice, anyDaycareCondition);
            string firstUpgradeCondition = GetDaycareTierCondition(
                BuildingContent.SlimeDaycareUpgrade1Id,
                BuildingContent.SlimeDaycareUpgrade2Id,
                BuildingContent.SlimeDaycareUpgrade3Id);
            string finalUpgradeCondition = GetDaycareTierCondition(BuildingContent.SlimeDaycareUpgrade3Id);
            AddIfMissing(shop, "SlimeTunnel", FurnitureContent.SlimeTunnelQualifiedId, config.SlimeTunnelRecipePrice, firstUpgradeCondition);
            AddIfMissing(shop, "HeatedSlimeStone", FurnitureContent.HeatedSlimeStoneQualifiedId, config.HeatedSlimeStoneRecipePrice, anyDaycareCondition);
            AddIfMissing(shop, "SlimePond", FurnitureContent.SlimePondQualifiedId, config.SlimePondRecipePrice, firstUpgradeCondition);
            AddIfMissing(shop, "SlimeSwing", FurnitureContent.SlimeSwingQualifiedId, config.SlimeSwingRecipePrice, firstUpgradeCondition);
            AddIfMissing(shop, "SlimeLamp", FurnitureContent.SlimeLampQualifiedId, config.SlimeLampRecipePrice, firstUpgradeCondition);
            AddIfMissing(shop, "SlimeFairyLights", FurnitureContent.SlimeFairyLightsQualifiedId, config.SlimeFairyLightsRecipePrice, finalUpgradeCondition);
            AddIfMissing(shop, "SlimeBallPit", FurnitureContent.SlimeBallPitQualifiedId, config.SlimeBallPitRecipePrice, finalUpgradeCondition);
        });
    }

    private static string GetDaycareTierCondition(params string[] tierIds) =>
        $"ANY {string.Join(" ", tierIds.Select(id => $"\"BUILDINGS_CONSTRUCTED All {id} 1 2147483647 true\""))}";

    private static void AddIfMissing(ShopData shop, string name, string qualifiedItemId, int price, string? requiredCondition = null)
    {
        string id = $"Taum.SlimeRanching_{name}";
        ShopItemData? existing = shop.Items!.FirstOrDefault(item => item.Id == id || item.ItemId == qualifiedItemId);
        if (existing is not null)
        {
            existing.Price = Math.Max(0, price);
            existing.IsRecipe = true;
            existing.AvailableStock = int.MaxValue;
            existing.AvailableStockLimit = LimitedStockMode.None;
            if (requiredCondition is not null)
                existing.Condition = AddCondition(existing.Condition, requiredCondition);
            return;
        }

        shop.Items.Add(new ShopItemData
        {
            Id = id,
            ItemId = qualifiedItemId,
            Price = Math.Max(0, price),
            IsRecipe = true,
            AvailableStock = int.MaxValue,
            AvailableStockLimit = LimitedStockMode.None,
            Condition = requiredCondition
        });
    }

    private static string AddCondition(string? existingCondition, string requiredCondition)
    {
        if (string.IsNullOrWhiteSpace(existingCondition))
            return requiredCondition;

        return existingCondition.Contains(requiredCondition, StringComparison.Ordinal)
            ? existingCondition
            : $"{existingCondition},{requiredCondition}";
    }
}
