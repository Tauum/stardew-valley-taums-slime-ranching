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
    public const string SlimeCushionItemId = "Taum.SlimeRanching_SlimeCushion";
    public const string SlimeCushionQualifiedId = "(BC)" + SlimeCushionItemId;
    public const string SlimePlaygroundItemId = "Taum.SlimeRanching_SlimePlayground";
    public const string SlimePlaygroundQualifiedId = "(BC)" + SlimePlaygroundItemId;
    private const string TrampolineTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeTrampoline";
    private const string CushionTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeCushion";
    private const string PlaygroundTexture = "Mods/Taum.SlimeRanching/Furniture/SlimePlayground";

    public static void LoadAssets(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(TrampolineTexture))
            e.LoadFrom(() => CreateTrampolineTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo(CushionTexture))
            e.LoadFrom(() => CreateCushionTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo(PlaygroundTexture))
            e.LoadFrom(() => CreatePlaygroundTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
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
                Description = "A springy platform that nearby Ranch Slimes can bounce on.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = TrampolineTexture,
                SpriteIndex = 0
            });
            craftables.TryAdd(SlimeCushionItemId, new BigCraftableData
            {
                Name = "Slime Cushion",
                DisplayName = "Slime Cushion",
                Description = "A soft spot where Ranch Slimes can settle into a gentle squish and spring back.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = CushionTexture,
                SpriteIndex = 0
            });
            craftables.TryAdd(SlimePlaygroundItemId, new BigCraftableData
            {
                Name = "Slime Playground",
                DisplayName = "Slime Playground",
                Description = "A colorful ball pit where two Ranch Slimes can play together.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = PlaygroundTexture,
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

    private static Texture2D CreateCushionTexture(GraphicsDevice graphicsDevice)
    {
        const int width = 16;
        const int height = 32;
        var texture = new Texture2D(graphicsDevice, width, height);
        var pixels = new Color[width * height];
        Color outline = new(90, 45, 67);
        Color dark = new(155, 69, 111);
        Color fill = new(226, 126, 165);
        Color light = new(247, 174, 194);

        for (int x = 4; x <= 11; x++) SetPixel(pixels, width, x, 10, outline);
        for (int y = 11; y <= 14; y++)
        {
            SetPixel(pixels, width, 3, y, outline);
            SetPixel(pixels, width, 12, y, outline);
            for (int x = 4; x <= 11; x++)
                SetPixel(pixels, width, x, y, y == 11 ? light : fill);
        }
        for (int x = 4; x <= 11; x++) SetPixel(pixels, width, x, 15, dark);
        SetPixel(pixels, width, 2, 12, dark);
        SetPixel(pixels, width, 13, 12, dark);
        for (int x = 5; x <= 10; x++) SetPixel(pixels, width, x, 16, new Color(67, 43, 56));
        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D CreatePlaygroundTexture(GraphicsDevice graphicsDevice)
    {
        const int width = 16;
        const int height = 32;
        var texture = new Texture2D(graphicsDevice, width, height);
        var pixels = new Color[width * height];
        Color outline = new(75, 50, 76);
        for (int x = 2; x <= 13; x++)
        {
            SetPixel(pixels, width, x, 8, outline);
            SetPixel(pixels, width, x, 15, outline);
        }
        for (int y = 9; y <= 14; y++)
        {
            SetPixel(pixels, width, 1, y, outline);
            SetPixel(pixels, width, 14, y, outline);
            for (int x = 2; x <= 13; x++)
            {
                Color rim = x is 2 or 13 ? new Color(229, 98, 150) : new Color(247, 155, 190);
                SetPixel(pixels, width, x, y, rim);
            }
        }

        (int X, int Y, Color Color)[] balls =
        {
            (3, 12, new Color(101, 178, 232)), (5, 13, new Color(250, 215, 92)),
            (7, 11, new Color(145, 205, 120)), (9, 13, new Color(194, 142, 226)),
            (11, 12, new Color(247, 142, 102)), (6, 10, new Color(250, 215, 92)),
            (10, 10, new Color(101, 178, 232))
        };
        foreach (var ball in balls)
        {
            SetPixel(pixels, width, ball.X, ball.Y, new Color(75, 50, 76));
            SetPixel(pixels, width, ball.X, ball.Y - 1, ball.Color);
        }
        for (int x = 3; x <= 12; x++) SetPixel(pixels, width, x, 16, new Color(67, 43, 56));
        texture.SetData(pixels);
        return texture;
    }

    private static void SetPixel(Color[] pixels, int width, int x, int y, Color color) =>
        pixels[y * width + x] = color;
}
