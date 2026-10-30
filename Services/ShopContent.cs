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
            foreach (DaycareFurnitureRecipeListing listing in FurnitureSlotCatalog.GetRecipeListings())
            {
                string condition = GetDaycareTierCondition(
                    BuildingContent.GetBuildingIdsAtOrAboveTier(listing.RequiredBuildingTier).ToArray());
                AddIfMissing(shop, listing.ShopItemName, listing.QualifiedItemId, listing.GetPrice(config), condition);
            }
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
