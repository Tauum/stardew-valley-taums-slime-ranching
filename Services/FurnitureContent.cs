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
    public const string SlimePondItemId = "Taum.SlimeRanching_SlimePond";
    public const string SlimePondQualifiedId = "(BC)" + SlimePondItemId;
    public const string SlimeSwingItemId = "Taum.SlimeRanching_SlimeSwing";
    public const string SlimeSwingQualifiedId = "(BC)" + SlimeSwingItemId;
    public const string SlimeSeesawItemId = "Taum.SlimeRanching_SlimeSeesaw";
    public const string SlimeSeesawQualifiedId = "(BC)" + SlimeSeesawItemId;
    public const string SlimeBallPitItemId = "Taum.SlimeRanching_SlimeBallPit";
    public const string SlimeBallPitQualifiedId = "(BC)" + SlimeBallPitItemId;
    public const string SlimeLampItemId = "Taum.SlimeRanching_SlimeLamp";
    public const string SlimeLampQualifiedId = "(BC)" + SlimeLampItemId;
    public const string SlimeFairyLightsItemId = "Taum.SlimeRanching_SlimeFairyLights";
    public const string SlimeFairyLightsQualifiedId = "(BC)" + SlimeFairyLightsItemId;
    public const string SlimeWashingMachineItemId = "Taum.SlimeRanching_SlimeWashingMachine";
    public const string SlimeWashingMachineQualifiedId = "(BC)" + SlimeWashingMachineItemId;
    private const string TrampolineTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeTrampoline";
    private const string CushionTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeCushion";
    private const string PlaygroundTexture = "Mods/Taum.SlimeRanching/Furniture/SlimePlayground";
    private const string SlideTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeSlide";
    private const string BedTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeBed";
    private const string CompostTroughTexture = "Mods/Taum.SlimeRanching/Furniture/CompostTrough";
    private const string TunnelTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeTunnel";
    private const string HeatedStoneTexture = "Mods/Taum.SlimeRanching/Furniture/HeatedSlimeStone";
    private const string PondTexture = "Mods/Taum.SlimeRanching/Furniture/SlimePond";
    private const string SwingTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeSwing";
    private const string SeesawTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeSeesaw";
    private const string BallPitTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeBallPit";
    private const string LampTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeLamp";
    private const string FairyLightsTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeFairyLights";
    private const string WashingMachineTexture = "Mods/Taum.SlimeRanching/Furniture/SlimeWashingMachine";

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
        else if (e.NameWithoutLocale.IsEquivalentTo(PondTexture))
            e.LoadFrom(() => CreatePondTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo(SwingTexture))
            e.LoadFrom(() => CreateSwingTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo(SeesawTexture))
            e.LoadFrom(() => CreateSeesawTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo(BallPitTexture))
            e.LoadFrom(() => CreateBallPitTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo(LampTexture))
            e.LoadFrom(() => CreateLampTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo(FairyLightsTexture))
            e.LoadFrom(() => CreateFairyLightsTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
        else if (e.NameWithoutLocale.IsEquivalentTo(WashingMachineTexture))
            e.LoadFrom(() => CreateWashingMachineTexture(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
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
                Description = "A springy platform that nearby Domesticated Slimes can bounce on.",
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
                Description = "A soft spot where Domesticated Slimes can settle into a gentle squish and spring back.",
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
                Description = "A colorful ball pit where two Domesticated Slimes can play together.",
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
                Description = "A little slide for Domesticated Slimes to climb and glide down.",
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
                Description = "A cozy bed where Domesticated Slimes can curl up for a rest.",
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
                Description = "Feed eligible scraps to a juvenile Domesticated Slime to help it grow.",
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
                Description = "A clear archway that Domesticated Slimes can crawl through.",
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
                Description = "A warm stone where Domesticated Slimes can huddle and rest.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = HeatedStoneTexture,
                SpriteIndex = 0
            });
            craftables.TryAdd(SlimePondItemId, new BigCraftableData
            {
                Name = "Slime Pond",
                DisplayName = "Slime Pond",
                Description = "A little pool where Domesticated Slimes can bob and splash.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = PondTexture,
                SpriteIndex = 0
            });
            craftables.TryAdd(SlimeSwingItemId, new BigCraftableData
            {
                Name = "Slime Swing",
                DisplayName = "Slime Swing",
                Description = "A little swing for Domesticated Slimes to sway on.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = SwingTexture,
                SpriteIndex = 0
            });
            craftables.TryAdd(SlimeSeesawItemId, new BigCraftableData
            {
                Name = "Slime Seesaw",
                DisplayName = "Slime Seesaw",
                Description = "A balanced beam where two Domesticated Slimes can play together.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = SeesawTexture,
                SpriteIndex = 0
            });
            craftables.TryAdd(SlimeBallPitItemId, new BigCraftableData
            {
                Name = "Slime Ball Pit",
                DisplayName = "Slime Ball Pit",
                Description = "A colorful pit where Domesticated Slimes can dip and pop back out.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = BallPitTexture,
                SpriteIndex = 0
            });
            craftables.TryAdd(SlimeLampItemId, new BigCraftableData
            {
                Name = "Slime Lamp",
                DisplayName = "Slime Lamp",
                Description = "A warm lamp where Domesticated Slimes can gather around the light.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                IsLamp = true,
                Texture = LampTexture,
                SpriteIndex = 0
            });
            craftables.TryAdd(SlimeFairyLightsItemId, new BigCraftableData
            {
                Name = "Slime Fairy Lights",
                DisplayName = "Slime Fairy Lights",
                Description = "A string of colorful lights that glows after dusk in the Slime Daycare.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = FairyLightsTexture,
                SpriteIndex = 0
            });
            craftables.TryAdd(SlimeWashingMachineItemId, new BigCraftableData
            {
                Name = "Slime Washing Machine",
                DisplayName = "Slime Washing Machine",
                Description = "A tidy little washing machine, sized for the Slime Daycare.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Fragility = 0,
                Texture = WashingMachineTexture,
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

    private static Texture2D CreatePondTexture(GraphicsDevice graphicsDevice)
    {
        const int width = 16;
        const int height = 32;
        var texture = new Texture2D(graphicsDevice, width, height);
        var pixels = new Color[width * height];
        Color outline = new(43, 58, 84);
        Color rim = new(104, 81, 125);
        Color water = new(67, 150, 194);
        Color highlight = new(154, 216, 233);
        for (int x = 2; x <= 13; x++)
        {
            SetPixel(pixels, width, x, 11, outline);
            SetPixel(pixels, width, x, 16, outline);
        }
        for (int y = 12; y <= 15; y++)
        {
            SetPixel(pixels, width, 1, y, outline);
            SetPixel(pixels, width, 14, y, outline);
            for (int x = 2; x <= 13; x++)
                SetPixel(pixels, width, x, y, y == 12 ? rim : water);
        }
        for (int x = 4; x <= 7; x++) SetPixel(pixels, width, x, 13, highlight);
        for (int x = 9; x <= 12; x++) SetPixel(pixels, width, x, 14, new Color(94, 181, 209));
        for (int x = 3; x <= 12; x++) SetPixel(pixels, width, x, 17, outline);
        SetPixel(pixels, width, 4, 9, highlight);
        SetPixel(pixels, width, 11, 9, highlight);
        for (int x = 4; x <= 11; x++) SetPixel(pixels, width, x, 18, new Color(67, 43, 56));
        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D CreateSwingTexture(GraphicsDevice graphicsDevice)
    {
        const int width = 16;
        const int height = 32;
        var texture = new Texture2D(graphicsDevice, width, height);
        var pixels = new Color[width * height];
        Color outline = new(67, 45, 42);
        Color wood = new(153, 103, 64);
        Color lightWood = new(196, 145, 91);
        Color rope = new(211, 190, 145);
        Color seat = new(206, 91, 139);
        for (int y = 4; y <= 13; y++)
        {
            SetPixel(pixels, width, 3, y, outline);
            SetPixel(pixels, width, 4, y, wood);
            SetPixel(pixels, width, 11, y, wood);
            SetPixel(pixels, width, 12, y, outline);
        }
        for (int x = 3; x <= 12; x++)
        {
            SetPixel(pixels, width, x, 4, outline);
            SetPixel(pixels, width, x, 5, lightWood);
        }
        for (int y = 6; y <= 12; y++)
        {
            SetPixel(pixels, width, 6, y, rope);
            SetPixel(pixels, width, 9, y, rope);
        }
        for (int x = 5; x <= 10; x++)
        {
            SetPixel(pixels, width, x, 13, outline);
            SetPixel(pixels, width, x, 14, seat);
            SetPixel(pixels, width, x, 15, outline);
        }
        for (int x = 4; x <= 11; x++) SetPixel(pixels, width, x, 16, new Color(67, 43, 56));
        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D CreateSeesawTexture(GraphicsDevice graphicsDevice)
    {
        const int width = 16;
        const int height = 32;
        var texture = new Texture2D(graphicsDevice, width, height);
        var pixels = new Color[width * height];
        Color outline = new(63, 47, 60);
        Color darkWood = new(112, 70, 53);
        Color wood = new(172, 118, 72);
        Color lightWood = new(215, 164, 104);
        Color seat = new(213, 102, 143);
        for (int x = 3; x <= 12; x++) SetPixel(pixels, width, x, 15, new Color(67, 43, 56));
        for (int y = 10; y <= 14; y++)
        {
            SetPixel(pixels, width, 7, y, outline);
            SetPixel(pixels, width, 8, y, darkWood);
        }
        for (int x = 5; x <= 10; x++)
        {
            SetPixel(pixels, width, x, 14, outline);
            if (x is 6 or 9)
                SetPixel(pixels, width, x, 13, wood);
        }
        for (int x = 1; x <= 14; x++)
        {
            SetPixel(pixels, width, x, 8, outline);
            SetPixel(pixels, width, x, 9, x is 2 or 13 ? seat : wood);
            SetPixel(pixels, width, x, 10, outline);
        }
        for (int x = 3; x <= 6; x++) SetPixel(pixels, width, x, 9, lightWood);
        for (int x = 9; x <= 12; x++) SetPixel(pixels, width, x, 9, lightWood);
        SetPixel(pixels, width, 2, 7, outline);
        SetPixel(pixels, width, 13, 7, outline);
        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D CreateBallPitTexture(GraphicsDevice graphicsDevice)
    {
        const int width = 16;
        const int height = 32;
        var texture = new Texture2D(graphicsDevice, width, height);
        var pixels = new Color[width * height];
        Color outline = new(59, 49, 74);
        Color rim = new(204, 91, 161);
        Color lightRim = new(242, 145, 192);
        Color blue = new(75, 159, 214);
        Color yellow = new(244, 202, 83);
        Color green = new(111, 190, 103);
        for (int x = 2; x <= 13; x++) SetPixel(pixels, width, x, 11, outline);
        for (int y = 12; y <= 16; y++)
        {
            SetPixel(pixels, width, 1, y, outline);
            SetPixel(pixels, width, 14, y, outline);
            SetPixel(pixels, width, 2, y, rim);
            SetPixel(pixels, width, 13, y, rim);
            for (int x = 3; x <= 12; x++)
                SetPixel(pixels, width, x, y, y == 12 ? new Color(121, 77, 158) : new Color(99, 66, 137));
        }
        for (int x = 2; x <= 13; x++)
        {
            SetPixel(pixels, width, x, 17, outline);
            SetPixel(pixels, width, x, 18, rim);
        }
        for (int x = 3; x <= 12; x++) SetPixel(pixels, width, x, 10, lightRim);
        SetPixel(pixels, width, 4, 13, blue);
        SetPixel(pixels, width, 7, 14, yellow);
        SetPixel(pixels, width, 10, 13, green);
        SetPixel(pixels, width, 12, 15, new Color(240, 110, 85));
        for (int x = 4; x <= 11; x++) SetPixel(pixels, width, x, 19, new Color(67, 43, 56));
        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D CreateLampTexture(GraphicsDevice graphicsDevice)
    {
        const int width = 16;
        const int height = 32;
        var texture = new Texture2D(graphicsDevice, width, height);
        var pixels = new Color[width * height];
        Color outline = new(62, 47, 59);
        Color brass = new(172, 111, 53);
        Color brightBrass = new(224, 166, 75);
        Color glass = new(245, 202, 105);
        Color glow = new(255, 240, 176);

        for (int x = 5; x <= 10; x++)
            SetPixel(pixels, width, x, 6, outline);
        for (int y = 7; y <= 14; y++)
        {
            int halfWidth = y <= 8 ? 2 : y <= 11 ? 3 : 4;
            for (int x = 8 - halfWidth; x <= 8 + halfWidth; x++)
            {
                bool edge = x == 8 - halfWidth || x == 8 + halfWidth;
                SetPixel(pixels, width, x, y, edge ? outline : (y <= 8 ? brightBrass : glass));
            }
        }
        SetPixel(pixels, width, 8, 9, glow);
        SetPixel(pixels, width, 9, 10, glow);
        for (int x = 3; x <= 12; x++)
        {
            SetPixel(pixels, width, x, 15, outline);
            if (x is >= 4 and <= 11)
                SetPixel(pixels, width, x, 16, x is 4 or 11 ? brass : brightBrass);
        }
        for (int y = 17; y <= 24; y++)
        {
            SetPixel(pixels, width, 7, y, outline);
            SetPixel(pixels, width, 8, y, y is 18 or 23 ? brightBrass : brass);
        }
        for (int x = 5; x <= 10; x++)
        {
            SetPixel(pixels, width, x, 25, outline);
            SetPixel(pixels, width, x, 26, x is 5 or 10 ? brass : brightBrass);
        }
        for (int x = 4; x <= 11; x++) SetPixel(pixels, width, x, 27, outline);
        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D CreateFairyLightsTexture(GraphicsDevice graphicsDevice)
    {
        const int width = 16;
        const int height = 32;
        var texture = new Texture2D(graphicsDevice, width, height);
        var pixels = new Color[width * height];
        Color wire = new(70, 55, 55);
        Color outline = new(66, 43, 58);
        Color[] bulbs = { new(246, 104, 133), new(112, 207, 237), new(255, 210, 102), new(175, 139, 237) };

        for (int x = 2; x <= 13; x++)
        {
            int y = x is 5 or 10 ? 7 : x is 3 or 12 ? 6 : 5;
            SetPixel(pixels, width, x, y, wire);
        }

        int[] bulbXs = { 3, 6, 9, 12 };
        for (int i = 0; i < bulbXs.Length; i++)
        {
            int x = bulbXs[i];
            int y = x is 3 or 12 ? 6 : 7;
            SetPixel(pixels, width, x, y, outline);
            SetPixel(pixels, width, x, y + 1, bulbs[i]);
            SetPixel(pixels, width, x, y + 2, bulbs[i]);
            SetPixel(pixels, width, x, y + 3, new Color(255, 230, 190));
        }

        for (int x = 1; x <= 14; x++)
            SetPixel(pixels, width, x, 11, new Color(67, 43, 56));
        texture.SetData(pixels);
        return texture;
    }

    private static Texture2D CreateWashingMachineTexture(GraphicsDevice graphicsDevice)
    {
        const int width = 16;
        const int height = 32;
        var texture = new Texture2D(graphicsDevice, width, height);
        var pixels = new Color[width * height];
        Color outline = new(61, 49, 62);
        Color darkMetal = new(109, 119, 137);
        Color metal = new(188, 197, 205);
        Color lightMetal = new(229, 231, 225);
        Color glass = new(91, 157, 183);
        Color brightGlass = new(139, 206, 218);
        Color accent = new(226, 114, 153);

        for (int y = 5; y <= 25; y++)
        {
            SetPixel(pixels, width, 3, y, outline);
            SetPixel(pixels, width, 12, y, outline);
            for (int x = 4; x <= 11; x++)
                SetPixel(pixels, width, x, y, x <= 5 ? lightMetal : metal);
        }
        for (int x = 4; x <= 11; x++)
        {
            SetPixel(pixels, width, x, 4, outline);
            SetPixel(pixels, width, x, 26, outline);
        }
        for (int x = 5; x <= 10; x++)
            SetPixel(pixels, width, x, 7, x == 9 ? accent : darkMetal);

        for (int x = 5; x <= 10; x++)
        {
            SetPixel(pixels, width, x, 10, outline);
            SetPixel(pixels, width, x, 19, outline);
        }
        for (int y = 11; y <= 18; y++)
        {
            SetPixel(pixels, width, 4, y, outline);
            SetPixel(pixels, width, 11, y, outline);
            for (int x = 5; x <= 10; x++)
                SetPixel(pixels, width, x, y, x is 5 or 10 ? glass : darkMetal);
        }
        SetPixel(pixels, width, 6, 12, brightGlass);
        SetPixel(pixels, width, 7, 12, brightGlass);
        SetPixel(pixels, width, 9, 17, brightGlass);
        for (int x = 4; x <= 11; x++)
            SetPixel(pixels, width, x, 27, new Color(67, 43, 56));

        texture.SetData(pixels);
        return texture;
    }

    private static void SetPixel(Color[] pixels, int width, int x, int y, Color color) =>
        pixels[y * width + x] = color;
}
