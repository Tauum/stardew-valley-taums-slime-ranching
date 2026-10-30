using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;

namespace taums_slime_ranching.Services;

internal static class SlimePrinterContent
{
    public const string ItemId = "Taum.SlimeRanching_SlimePrinter";
    public const string QualifiedItemId = "(BC)" + ItemId;
    private const string TextureName = "Mods/Taum.SlimeRanching/Machines/SlimePrinter";

    public static bool IsSlimePrinter(StardewValley.Object obj) =>
        obj.QualifiedItemId == QualifiedItemId;

    public static void LoadAssets(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(TextureName))
            e.LoadFrom(() => CreateTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
    }

    public static void EditAsset(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
            return;

        e.Edit(asset =>
        {
            var craftables = asset.AsDictionary<string, BigCraftableData>().Data;
            craftables.TryAdd(ItemId, CreateBigCraftableData());
        });
    }

    private static BigCraftableData CreateBigCraftableData() => new()
    {
        Name = "Slime Printer",
        DisplayName = "Slime Printer",
        Description = "Recycle discarded items with vanilla Slime fuel.",
        CanBePlacedIndoors = true,
        CanBePlacedOutdoors = false,
        Fragility = 0,
        Texture = TextureName,
        SpriteIndex = 0
    };

    private static Texture2D CreateTexture(GraphicsDevice graphicsDevice)
    {
        const int width = 16;
        const int height = 32;
        var pixels = new Color[width * height];
        var texture = new Texture2D(graphicsDevice, width, height);
        Color outline = new(57, 48, 41);
        Color bronze = new(142, 103, 66);
        Color light = new(194, 155, 103);
        Color gel = new(171, 93, 199);
        for (int x = 4; x <= 11; x++)
        {
            pixels[7 * width + x] = outline;
            pixels[20 * width + x] = outline;
            pixels[24 * width + x] = outline;
        }
        for (int y = 8; y <= 19; y++)
        {
            pixels[y * width + 3] = outline;
            pixels[y * width + 12] = outline;
            for (int x = 4; x <= 11; x++)
                pixels[y * width + x] = y is 9 or 18 ? light : bronze;
        }
        for (int x = 5; x <= 10; x++) pixels[13 * width + x] = gel;
        for (int x = 6; x <= 9; x++) pixels[14 * width + x] = gel;
        pixels[11 * width + 5] = light;
        pixels[11 * width + 10] = light;
        for (int x = 5; x <= 10; x++) pixels[21 * width + x] = bronze;
        for (int x = 6; x <= 9; x++) pixels[22 * width + x] = outline;
        for (int x = 4; x <= 11; x++) pixels[25 * width + x] = outline;
        texture.SetData(pixels);
        return texture;
    }
}
