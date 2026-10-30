using StardewModdingAPI.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData.BigCraftables;

namespace taums_slime_ranching.Services;

/// <summary>Defines the Slime Breeder and Slime Incubator items.</summary>
internal static class RanchingItemContent
{
    private const string BreederItemId = "Taum.SlimeRanching_SlimeBreeder";
    private const string IncubatorItemId = "Taum.SlimeRanching_SlimeIncubator";
    private const string IconTextureName = "Mods/Taum.SlimeRanching/Machines/ModMachineIcons";

    public const string BreederQualifiedId = "(BC)" + BreederItemId;
    public const string IncubatorQualifiedId = "(BC)" + IncubatorItemId;

    public static void LoadAssets(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(IconTextureName))
            e.LoadFrom(() => CreateMachineIcons(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
    }

    public static void EditAsset(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
        {
            e.Edit(asset =>
            {
                var craftables = asset.AsDictionary<string, BigCraftableData>().Data;
                if (!craftables.ContainsKey(BreederItemId))
                {
                    craftables[BreederItemId] = new BigCraftableData
                    {
                        Name = "Slime Breeder",
                        DisplayName = "Slime Breeder",
                        Description = "Select two Domesticated Slimes for breeding.",
                        CanBePlacedIndoors = true,
                        CanBePlacedOutdoors = false,
                        Texture = IconTextureName,
                        SpriteIndex = 0
                    };
                }

                if (!craftables.ContainsKey(IncubatorItemId))
                {
                    craftables[IncubatorItemId] = new BigCraftableData
                    {
                        Name = "Slime Incubator",
                        DisplayName = "Slime Incubator",
                        Description = "Hatches a genome-specific Domesticated Slime Egg inside the Slime Daycare.",
                        CanBePlacedIndoors = true,
                        CanBePlacedOutdoors = false,
                        Texture = IconTextureName,
                        SpriteIndex = 1
                    };
                }
            });
        }
    }

    private static Texture2D CreateMachineIcons(GraphicsDevice graphicsDevice)
    {
        const int tileWidth = 16;
        const int tileHeight = 32;
        const int columns = 2;
        var pixels = new Color[tileWidth * columns * tileHeight];
        var texture = new Texture2D(graphicsDevice, tileWidth * columns, tileHeight);

        Color outline = new(28, 25, 48);
        Color steel = new(107, 113, 155);
        Color highlight = new(178, 193, 225);
        Color blue = new(45, 173, 230);
        Color pink = new(237, 92, 187);
        Color orange = new(248, 159, 65);
        // Breeder: blue and pink chambers under a heart marker.
        int breederX = 0;
        DrawMachineBody(pixels, tileWidth * columns, breederX, outline, steel, highlight);
        Fill(pixels, tileWidth * columns, breederX + 4, 12, 3, 8, blue);
        Fill(pixels, tileWidth * columns, breederX + 9, 12, 3, 8, pink);
        Fill(pixels, tileWidth * columns, breederX + 6, 7, 4, 3, pink);
        Set(pixels, tileWidth * columns, breederX + 7, 6, pink);
        Set(pixels, tileWidth * columns, breederX + 9, 6, pink);
        Fill(pixels, tileWidth * columns, breederX + 6, 10, 4, 1, orange);

        // Incubator: a single glowing violet egg chamber.
        int incubatorX = tileWidth;
        Texture2D incubatorArt = Game1.content.Load<Texture2D>("Mods/Taum.SlimeRanching/Machines/SlimeIncubator");
        DrawMachineArtIcon(pixels, tileWidth * columns, incubatorX, incubatorArt);

        texture.SetData(pixels);
        return texture;
    }

    private static void DrawMachineArtIcon(Color[] destination, int destinationWidth, int destinationX, Texture2D source)
    {
        const int iconWidth = 14;
        const int iconHeight = 23;
        const int iconOffsetX = 1;
        const int iconOffsetY = 4;
        Color[] sourcePixels = new Color[source.Width * source.Height];
        source.GetData(sourcePixels);

        int left = source.Width;
        int top = source.Height;
        int right = -1;
        int bottom = -1;
        for (int y = 0; y < source.Height; y++)
        for (int x = 0; x < source.Width; x++)
        {
            if (sourcePixels[y * source.Width + x].A == 0)
                continue;

            left = Math.Min(left, x);
            top = Math.Min(top, y);
            right = Math.Max(right, x);
            bottom = Math.Max(bottom, y);
        }

        if (right < left || bottom < top)
            return;

        int sourceWidth = right - left + 1;
        int sourceHeight = bottom - top + 1;
        for (int y = 0; y < iconHeight; y++)
        for (int x = 0; x < iconWidth; x++)
        {
            int sourceX = left + Math.Min(sourceWidth - 1, x * sourceWidth / iconWidth);
            int sourceY = top + Math.Min(sourceHeight - 1, y * sourceHeight / iconHeight);
            destination[(y + iconOffsetY) * destinationWidth + destinationX + iconOffsetX + x] =
                sourcePixels[sourceY * source.Width + sourceX];
        }
    }

    private static void DrawMachineBody(Color[] pixels, int sheetWidth, int offsetX, Color outline, Color steel, Color highlight)
    {
        Fill(pixels, sheetWidth, offsetX + 3, 9, 10, 17, outline);
        Fill(pixels, sheetWidth, offsetX + 4, 10, 8, 14, steel);
        Fill(pixels, sheetWidth, offsetX + 5, 11, 6, 1, highlight);
        Fill(pixels, sheetWidth, offsetX + 2, 25, 12, 3, outline);
        Fill(pixels, sheetWidth, offsetX + 4, 28, 8, 2, outline);
        Fill(pixels, sheetWidth, offsetX + 5, 27, 6, 1, steel);
        Set(pixels, sheetWidth, offsetX + 4, 30, steel);
        Set(pixels, sheetWidth, offsetX + 11, 30, steel);
    }

    private static void Fill(Color[] pixels, int sheetWidth, int x, int y, int width, int height, Color color)
    {
        for (int dy = 0; dy < height; dy++)
        for (int dx = 0; dx < width; dx++)
            Set(pixels, sheetWidth, x + dx, y + dy, color);
    }

    private static void Set(Color[] pixels, int sheetWidth, int x, int y, Color color) =>
        pixels[y * sheetWidth + x] = color;
}
