using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;

namespace taums_slime_ranching.Services;

/// <summary>Custom placeable enrichment furniture definitions and small pixel-art assets.</summary>
internal static class FurnitureContent
{
    public const string TrampolineItemId = "Taum.SlimeRanching_SlimeTrampoline";
    public const string TrampolineQualifiedId = "(BC)" + TrampolineItemId;
    private const string TrampolineTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeTrampoline";

    public static void LoadAssets(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(TrampolineTexture))
            e.LoadFrom(() => CreateTrampolineTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
    }

    public static void EditAsset(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
            return;

        e.Edit(asset =>
        {
            var craftables = asset.AsDictionary<string, BigCraftableData>().Data;
            craftables.TryAdd(TrampolineItemId, new BigCraftableData
            {
                Name = "Slime Trampoline",
                DisplayName = "Slime Trampoline",
                Description = "A bouncy platform for Ranch Slimes. Slime interactions are coming next.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = TrampolineTexture,
                SpriteIndex = 0
            });
        });
    }

    private static Texture2D CreateTrampolineTexture(GraphicsDevice graphicsDevice)
    {
        const int width = 16;
        const int height = 32;
        var texture = new Texture2D(graphicsDevice, width, height);
        var pixels = new Color[width * height];

        // Raised pink trampoline rim, drawn as clean 16-bit-style pixel art.
        int[] halfWidths = { 3, 5, 6, 7, 7, 7, 6, 5 };
        for (int row = 0; row < halfWidths.Length; row++)
        {
            int y = row + 2;
            int left = 8 - halfWidths[row];
            int right = 8 + halfWidths[row];
            SetPixel(pixels, width, left, y, new Color(100, 36, 82));
            SetPixel(pixels, width, right, y, new Color(100, 36, 82));
            for (int x = left + 1; x < right; x++)
            {
                Color fill = row < 2 ? new Color(231, 102, 161) : new Color(209, 68, 138);
                if (x >= 7 && x <= 9 && row >= 2 && row <= 5)
                    fill = new Color(245, 139, 186);
                SetPixel(pixels, width, x, y, fill);
            }
        }

        // Short wooden legs and a dark shadow keep the thin furniture legible in-game.
        for (int y = 10; y <= 14; y++)
        {
            SetPixel(pixels, width, 4, y, new Color(91, 51, 48));
            SetPixel(pixels, width, 11, y, new Color(91, 51, 48));
        }
        for (int x = 3; x <= 12; x++)
            SetPixel(pixels, width, x, 15, new Color(67, 43, 56));

        texture.SetData(pixels);
        return texture;
    }

    private static void SetPixel(Color[] pixels, int width, int x, int y, Color color) =>
        pixels[y * width + x] = color;
}
