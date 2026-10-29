using StardewModdingAPI.Events;
using StardewValley.GameData.Shops;

namespace taums_slime_ranching.Services;

/// <summary>Adds the current prototype furniture to Marnie's existing shop.</summary>
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
            AddIfMissing(shop, "Trampoline", FurnitureContent.TrampolineQualifiedId, config.TrampolineShopPrice);
            AddIfMissing(shop, "SlimeCushion", FurnitureContent.SlimeCushionQualifiedId, config.SlimeCushionShopPrice);
            AddIfMissing(shop, "SlimeSlide", FurnitureContent.SlimeSlideQualifiedId, config.SlimeSlideShopPrice);
            AddIfMissing(shop, "SlimeTunnel", FurnitureContent.SlimeTunnelQualifiedId, config.SlimeTunnelShopPrice);
            AddIfMissing(shop, "HeatedSlimeStone", FurnitureContent.HeatedSlimeStoneQualifiedId, config.HeatedSlimeStoneShopPrice);
            AddIfMissing(shop, "SlimePond", FurnitureContent.SlimePondQualifiedId, config.SlimePondShopPrice);
            AddIfMissing(shop, "SlimeSwing", FurnitureContent.SlimeSwingQualifiedId, config.SlimeSwingShopPrice);
            AddIfMissing(shop, "SlimeFairyLights", FurnitureContent.SlimeFairyLightsQualifiedId, config.SlimeFairyLightsShopPrice);
        });
    }

    private static void AddIfMissing(ShopData shop, string name, string qualifiedItemId, int price)
    {
        string id = $"Taum.SlimeRanching_{name}";
        ShopItemData? existing = shop.Items!.FirstOrDefault(item => item.Id == id || item.ItemId == qualifiedItemId);
        if (existing is not null)
        {
            existing.Price = Math.Max(0, price);
            existing.AvailableStock = int.MaxValue;
            existing.AvailableStockLimit = LimitedStockMode.None;
            return;
        }

        shop.Items.Add(new ShopItemData
        {
            Id = id,
            ItemId = qualifiedItemId,
            Price = Math.Max(0, price),
            AvailableStock = int.MaxValue,
            AvailableStockLimit = LimitedStockMode.None
        });
    }
}
