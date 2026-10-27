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
    public const string SlimeSlideItemId = "Taum.SlimeRanching_SlimeSlide";
    public const string SlimeSlideQualifiedId = "(BC)" + SlimeSlideItemId;
    public const string SlimeBedItemId = "Taum.SlimeRanching_SlimeBed";
    public const string SlimeBedQualifiedId = "(BC)" + SlimeBedItemId;
    public const string CompostTroughItemId = "Taum.SlimeRanching_CompostTrough";
    public const string CompostTroughQualifiedId = "(BC)" + CompostTroughItemId;
    public const string SlimeTunnelItemId = "Taum.SlimeRanching_SlimeTunnel";
    public const string SlimeTunnelQualifiedId = "(BC)" + SlimeTunnelItemId;
    public const string HeatedSlimeStoneItemId = "Taum.SlimeRanching_HeatedSlimeStone";
    public const string HeatedSlimeStoneQualifiedId = "(BC)" + HeatedSlimeStoneItemId;
    private const string TrampolineTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeTrampoline";
    private const string CushionTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeCushion";
    private const string PlaygroundTexture = "Mods/Taum.SlimeRanching/Furniture/SlimePlayground";
    private const string SlideTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeSlide";
    private const string BedTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeBed";
    private const string CompostTroughTexture = "Mods/Taum.SlimeRanching/Furniture/CompostTrough";
    private const string TunnelTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeTunnel";
    private const string HeatedStoneTexture = "Mods/Taum.SlimeRanching/Furniture/HeatedSlimeStone";

    public static void LoadAssets(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(TrampolineTexture))
            e.LoadFrom(() => CreateTrampolineTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo(CushionTexture))
            e.LoadFrom(() => CreateCushionTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo(PlaygroundTexture))
            e.LoadFrom(() => CreatePlaygroundTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo(SlideTexture))
            e.LoadFrom(() => CreateSlideTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo(BedTexture))
            e.LoadFrom(() => CreateBedTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo(CompostTroughTexture))
            e.LoadFrom(() => CreateCompostTroughTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo(TunnelTexture))
            e.LoadFrom(() => CreateTunnelTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo(HeatedStoneTexture))
            e.LoadFrom(() => CreateHeatedStoneTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
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
            craftables.TryAdd(SlimeSlideItemId, new BigCraftableData
            {
                Name = "Slime Slide",
                DisplayName = "Slime Slide",
                Description = "A little slide for Ranch Slimes to climb and glide down.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = SlideTexture,
                SpriteIndex = 0
            });
            craftables.TryAdd(SlimeBedItemId, new BigCraftableData
            {
                Name = "Slime Bed",
                DisplayName = "Slime Bed",
                Description = "A cozy bed where Ranch Slimes can curl up for a rest.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = BedTexture,
                SpriteIndex = 0
            });
            craftables.TryAdd(CompostTroughItemId, new BigCraftableData
            {
                Name = "Compost Trough",
                DisplayName = "Compost Trough",
                Description = "Feed eligible scraps to a juvenile Ranch Slime to help it grow.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = CompostTroughTexture,
                SpriteIndex = 0
            });
            craftables.TryAdd(SlimeTunnelItemId, new BigCraftableData
            {
                Name = "Slime Tunnel",
                DisplayName = "Slime Tunnel",
                Description = "A clear archway that Ranch Slimes can crawl through.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = TunnelTexture,
                SpriteIndex = 0
            });
            craftables.TryAdd(HeatedSlimeStoneItemId, new BigCraftableData
            {
                Name = "Heated Slime Stone",
                DisplayName = "Heated Slime Stone",
                Description = "A warm stone where Ranch Slimes can huddle and rest.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = HeatedStoneTexture,
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

    private static Texture2D CreateSlideTexture(GraphicsDevice graphicsDevice)
    {
        const int width = 16;
        const int height = 32;
        var texture = new Texture2D(graphicsDevice, width, height);
        var pixels = new Color[width * height];
        Color outline = new(77, 55, 86);
        Color rail = new(113, 96, 162);
        Color surface = new(167, 151, 220);
        Color highlight = new(211, 199, 244);
        for (int y = 4; y <= 12; y++)
        {
            int left = y - 2;
            int right = Math.Min(14, left + 4);
            SetPixel(pixels, width, left - 1, y, outline);
            SetPixel(pixels, width, right + 1, y, outline);
            SetPixel(pixels, width, left, y, rail);
            for (int x = left + 1; x < right; x++)
                SetPixel(pixels, width, x, y, x == left + 1 ? highlight : surface);
            SetPixel(pixels, width, right, y, rail);
        }
        for (int x = 8; x <= 13; x++)
            SetPixel(pixels, width, x, 13, outline);
        SetPixel(pixels, width, 4, 13, new Color(107, 75, 55));
        SetPixel(pixels, width, 11, 13, new Color(107, 75, 55));
        for (int x = 3; x <= 12; x++) SetPixel(pixels, width, x, 14, new Color(67, 43, 56));
        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D CreateBedTexture(GraphicsDevice graphicsDevice)
    {
        const int width = 16;
        const int height = 32;
        var texture = new Texture2D(graphicsDevice, width, height);
        var pixels = new Color[width * height];
        Color outline = new(83, 48, 70);
        Color wood = new(139, 83, 65);
        Color blanket = new(199, 96, 151);
        Color light = new(239, 160, 193);
        for (int x = 2; x <= 13; x++) SetPixel(pixels, width, x, 10, outline);
        for (int y = 11; y <= 14; y++)
        {
            SetPixel(pixels, width, 1, y, outline);
            SetPixel(pixels, width, 14, y, outline);
            for (int x = 2; x <= 13; x++)
                SetPixel(pixels, width, x, y, x <= 5 ? light : blanket);
        }
        for (int x = 2; x <= 13; x++) SetPixel(pixels, width, x, 15, wood);
        for (int y = 16; y <= 18; y++)
        {
            SetPixel(pixels, width, 3, y, outline);
            SetPixel(pixels, width, 12, y, outline);
            SetPixel(pixels, width, 4, y, wood);
            SetPixel(pixels, width, 11, y, wood);
        }
        for (int x = 2; x <= 13; x++) SetPixel(pixels, width, x, 19, new Color(67, 43, 56));
        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D CreateCompostTroughTexture(GraphicsDevice graphicsDevice)
    {
        const int width = 16;
        const int height = 32;
        var texture = new Texture2D(graphicsDevice, width, height);
        var pixels = new Color[width * height];
        Color outline = new(72, 50, 39);
        Color wood = new(145, 96, 58);
        Color lightWood = new(188, 134, 80);
        Color compost = new(92, 116, 54);
        for (int x = 3; x <= 12; x++)
        {
            pixels[10 * width + x] = outline;
            pixels[15 * width + x] = outline;
        }
        for (int y = 11; y <= 14; y++)
        {
            pixels[y * width + 2] = outline;
            pixels[y * width + 13] = outline;
            for (int x = 3; x <= 12; x++)
                pixels[y * width + x] = (y == 11 || x is 3 or 12) ? lightWood : wood;
        }
        for (int x = 4; x <= 11; x++) pixels[12 * width + x] = compost;
        pixels[11 * width + 5] = new Color(140, 153, 76);
        pixels[11 * width + 9] = new Color(140, 153, 76);
        for (int x = 4; x <= 11; x++) pixels[16 * width + x] = outline;
        for (int y = 17; y <= 19; y++)
        {
            pixels[y * width + 4] = outline;
            pixels[y * width + 11] = outline;
        }
        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D CreateTunnelTexture(GraphicsDevice graphicsDevice)
    {
        const int width = 16;
        const int height = 32;
        var texture = new Texture2D(graphicsDevice, width, height);
        var pixels = new Color[width * height];
        Color outline = new(59, 65, 82);
        Color frame = new(134, 106, 74);
        Color highlight = new(195, 170, 125);
        Color glass = new(136, 202, 222, 110);
        for (int x = 5; x <= 10; x++) SetPixel(pixels, width, x, 6, outline);
        for (int x = 3; x <= 12; x++) SetPixel(pixels, width, x, 7, frame);
        for (int x = 2; x <= 13; x++)
        {
            SetPixel(pixels, width, x, 8, outline);
            SetPixel(pixels, width, x, 9, highlight);
        }
        for (int y = 9; y <= 17; y++)
        {
            SetPixel(pixels, width, 2, y, outline);
            SetPixel(pixels, width, 3, y, frame);
            SetPixel(pixels, width, 12, y, frame);
            SetPixel(pixels, width, 13, y, outline);
            for (int x = 4; x <= 11; x++) SetPixel(pixels, width, x, y, glass);
        }
        for (int x = 2; x <= 13; x++)
        {
            SetPixel(pixels, width, x, 18, outline);
            SetPixel(pixels, width, x, 19, frame);
        }
        SetPixel(pixels, width, 5, 10, new Color(239, 246, 255, 190));
        SetPixel(pixels, width, 6, 11, new Color(239, 246, 255, 190));
        for (int x = 3; x <= 12; x++) SetPixel(pixels, width, x, 20, new Color(67, 43, 56));
        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D CreateHeatedStoneTexture(GraphicsDevice graphicsDevice)
    {
        const int width = 16;
        const int height = 32;
        var texture = new Texture2D(graphicsDevice, width, height);
        var pixels = new Color[width * height];
        Color outline = new(66, 48, 47);
        Color stone = new(103, 91, 91);
        Color lightStone = new(150, 126, 111);
        Color ember = new(219, 98, 58);
        Color glow = new(246, 172, 78);
        SetPixel(pixels, width, 7, 6, ember);
        SetPixel(pixels, width, 8, 5, glow);
        SetPixel(pixels, width, 9, 6, ember);
        for (int x = 4; x <= 11; x++)
        {
            SetPixel(pixels, width, x, 9, outline);
            SetPixel(pixels, width, x, 15, outline);
        }
        for (int y = 10; y <= 14; y++)
        {
            SetPixel(pixels, width, 3, y, outline);
            SetPixel(pixels, width, 12, y, outline);
            for (int x = 4; x <= 11; x++)
                SetPixel(pixels, width, x, y, y is 10 or 14 ? lightStone : stone);
        }
        for (int x = 5; x <= 10; x++) SetPixel(pixels, width, x, 16, new Color(67, 43, 56));
        texture.SetData(pixels);
        return texture;
    }

    private static void SetPixel(Color[] pixels, int width, int x, int y, Color color) =>
        pixels[y * width + x] = color;
}
