using StardewModdingAPI.Events;
using StardewValley.GameData.Shops;

namespace taums_slime_ranching.Services;

/// <summary>Adds the mod's core ranching machines to Marnie's existing shop.</summary>
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
            AddIfMissing(shop, "Converter", ConverterContent.ConverterQualifiedId, config.ConverterShopPrice);
            AddIfMissing(shop, "Breeder", ConverterContent.BreederQualifiedId, config.BreederShopPrice);
            AddIfMissing(shop, "Incubator", ConverterContent.IncubatorQualifiedId, config.IncubatorShopPrice);
            AddIfMissing(shop, "Trampoline", FurnitureContent.TrampolineQualifiedId, config.TrampolineShopPrice);
            AddIfMissing(shop, "SlimeCushion", FurnitureContent.SlimeCushionQualifiedId, config.SlimeCushionShopPrice);
            AddIfMissing(shop, "SlimePlayground", FurnitureContent.SlimePlaygroundQualifiedId, config.SlimePlaygroundShopPrice);
            AddIfMissing(shop, "SlimeSlide", FurnitureContent.SlimeSlideQualifiedId, config.SlimeSlideShopPrice);
            AddIfMissing(shop, "SlimeBed", FurnitureContent.SlimeBedQualifiedId, config.SlimeBedShopPrice);
            AddIfMissing(shop, "MutagenicPool", MutagenicPoolContent.QualifiedItemId, config.MutagenicPoolShopPrice);
            AddIfMissing(shop, "GelRefinery", GelRefineryContent.QualifiedItemId, config.GelRefineryShopPrice);
            AddIfMissing(shop, "CompostTrough", FurnitureContent.CompostTroughQualifiedId, config.CompostTroughShopPrice);
            AddIfMissing(shop, "SlimeTunnel", FurnitureContent.SlimeTunnelQualifiedId, config.SlimeTunnelShopPrice);
            AddIfMissing(shop, "HeatedSlimeStone", FurnitureContent.HeatedSlimeStoneQualifiedId, config.HeatedSlimeStoneShopPrice);
            AddIfMissing(shop, "SlimePond", FurnitureContent.SlimePondQualifiedId, config.SlimePondShopPrice);
            AddIfMissing(shop, "SlimeSwing", FurnitureContent.SlimeSwingQualifiedId, config.SlimeSwingShopPrice);
        });
    }

    private static void AddIfMissing(ShopData shop, string name, string qualifiedItemId, int price)
    {
        string id = $"Taum.SlimeRanching_{name}";
        if (shop.Items!.Any(item => item.Id == id || item.ItemId == qualifiedItemId))
            return;

        shop.Items.Add(new ShopItemData
        {
            Id = id,
            ItemId = qualifiedItemId,
            Price = Math.Max(0, price),
            AvailableStock = 1,
            AvailableStockLimit = LimitedStockMode.Player
        });
    }
}
