using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;

namespace taums_slime_ranching.Services;

internal static class MutagenicPoolContent
{
    public const string ItemId = "Taum.SlimeRanching_MutagenicPool";
    public const string QualifiedItemId = "(BC)" + ItemId;
    private const string TextureName = "Mods/Taum.SlimeRanching/Machines/MutagenicPool";

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
            craftables.TryAdd(ItemId, new BigCraftableData
            {
                Name = "Mutagenic Pool",
                DisplayName = "Mutagenic Pool",
                Description = "Uses vanilla Slime to try to awaken a mutation in a Ranch Slime.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = TextureName,
                SpriteIndex = 0
            });
        });
    }

    private static Texture2D CreateTexture(GraphicsDevice graphicsDevice)
    {
        const int width = 16;
        const int height = 32;
        var pixels = new Color[width * height];
        var texture = new Texture2D(graphicsDevice, width, height);
        Color outline = new(63, 42, 75);
        Color pool = new(112, 68, 150);
        Color liquid = new(170, 97, 205);
        Color glint = new(221, 157, 235);
        for (int x = 4; x <= 11; x++)
        {
            pixels[8 * width + x] = outline;
            pixels[15 * width + x] = outline;
        }
        for (int y = 9; y <= 14; y++)
        {
            pixels[y * width + 3] = outline;
            pixels[y * width + 12] = outline;
            for (int x = 4; x <= 11; x++)
                pixels[y * width + x] = y == 10 && x < 7 ? glint : pool;
        }
        for (int x = 5; x <= 10; x++) pixels[11 * width + x] = liquid;
        pixels[12 * width + 6] = glint;
        pixels[12 * width + 9] = glint;
        pixels[16 * width + 4] = outline;
        pixels[16 * width + 11] = outline;
        pixels[17 * width + 5] = outline;
        pixels[17 * width + 10] = outline;
        for (int x = 4; x <= 11; x++) pixels[18 * width + x] = outline;
        texture.SetData(pixels);
        return texture;
    }
}
