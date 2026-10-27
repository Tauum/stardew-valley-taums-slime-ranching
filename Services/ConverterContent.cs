using StardewModdingAPI.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;

namespace taums_slime_ranching.Services;

/// <summary>Defines a distinct mod egg and a machine that accepts only vanilla slime eggs.</summary>
internal static class ConverterContent
{
    private const string CommonEggItemId = "Taum.SlimeRanching_CommonEgg";
    private const string ConverterItemId = "Taum.SlimeRanching_EggConverter";
    private const string BreederItemId = "Taum.SlimeRanching_SlimeBreeder";
    private const string IncubatorItemId = "Taum.SlimeRanching_SlimeIncubator";
    private const string IconTextureName = "Mods/Taum.SlimeRanching/Machines/ModMachineIcons";

    public const string CommonEggQualifiedId = "(O)" + CommonEggItemId;
    public const string ConverterQualifiedId = "(BC)" + ConverterItemId;
    public const string BreederQualifiedId = "(BC)" + BreederItemId;
    public const string IncubatorQualifiedId = "(BC)" + IncubatorItemId;

    private static readonly string[] VanillaEggIds =
    {
        "(O)680", // Green
        "(O)413", // Blue
        "(O)437", // Red
        "(O)439", // Purple
        "(O)857"  // Tiger
    };

    public static void LoadAssets(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(IconTextureName))
            e.LoadFrom(() => CreateMachineIcons(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
    }

    public static void EditAsset(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
        {
            e.Edit(asset =>
            {
                var objects = asset.AsDictionary<string, ObjectData>().Data;
                if (!objects.ContainsKey(CommonEggItemId))
                {
                    objects[CommonEggItemId] = new ObjectData
                    {
                        Name = "Common Slime Egg",
                        DisplayName = "Common Slime Egg",
                        Description = "A common Ranch Slime egg made by a converter. Hatches into a base Common Ranch Slime.",
                        Type = "Basic",
                        Price = 1000,
                        Texture = "Maps/springobjects",
                        SpriteIndex = 680,
                        CanBeGivenAsGift = false,
                        CanBeTrashed = true,
                        ExcludeFromShippingCollection = true,
                        ContextTags = new List<string> { "taum_common_slime_egg" }
                    };
                }
            });
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
        {
            e.Edit(asset =>
            {
                var craftables = asset.AsDictionary<string, BigCraftableData>().Data;
                if (!craftables.ContainsKey(ConverterItemId))
                {
                    craftables[ConverterItemId] = new BigCraftableData
                    {
                        Name = "Slime Egg Converter",
                        DisplayName = "Slime Egg Converter",
                        Description = "Turns one vanilla Slime Egg into one Common Slime Egg.",
                        CanBePlacedIndoors = true,
                        CanBePlacedOutdoors = true,
                        Texture = IconTextureName,
                        SpriteIndex = 0
                    };
                }

                if (!craftables.ContainsKey(BreederItemId))
                {
                    craftables[BreederItemId] = new BigCraftableData
                    {
                        Name = "Slime Breeder",
                        DisplayName = "Slime Breeder",
                        Description = "Select two Ranch Slimes for breeding.",
                        CanBePlacedIndoors = true,
                        CanBePlacedOutdoors = false,
                        Texture = IconTextureName,
                        SpriteIndex = 1
                    };
                }

                if (!craftables.ContainsKey(IncubatorItemId))
                {
                    craftables[IncubatorItemId] = new BigCraftableData
                    {
                        Name = "Slime Incubator",
                        DisplayName = "Slime Incubator",
                        Description = "Hatches a Common or genome-specific Ranch Slime Egg inside the Slime Daycare.",
                        CanBePlacedIndoors = true,
                        CanBePlacedOutdoors = false,
                        Texture = IconTextureName,
                        SpriteIndex = 2
                    };
                }
            });
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
        {
            e.Edit(asset =>
            {
                var machines = asset.AsDictionary<string, MachineData>().Data;
                if (!machines.ContainsKey(ConverterQualifiedId))
                {
                    machines[ConverterQualifiedId] = new MachineData
                    {
                        // These are inferred from OutputRules, but setting them explicitly
                        // makes the converter's player-facing machine behavior unambiguous.
                        HasInput = true,
                        HasOutput = true,
                        OutputRules = new List<MachineOutputRule>
                        {
                            new()
                            {
                                Id = "ConvertVanillaSlimeEgg",
                                Triggers = VanillaEggIds.Select((id, index) => new MachineOutputTriggerRule
                                {
                                    Id = $"VanillaEgg{index}",
                                    Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                                    RequiredItemId = id,
                                    RequiredCount = 1
                                }).ToList(),
                                OutputItem = new List<MachineItemOutput>
                                {
                                    new() { ItemId = CommonEggQualifiedId, MinStack = 1, MaxStack = 1 }
                                },
                                MinutesUntilReady = 10
                            }
                        }
                    };
                }
            });
        }
    }

    private static Texture2D CreateMachineIcons(GraphicsDevice graphicsDevice)
    {
        const int tileWidth = 16;
        const int tileHeight = 32;
        const int columns = 3;
        var pixels = new Color[tileWidth * columns * tileHeight];
        var texture = new Texture2D(graphicsDevice, tileWidth * columns, tileHeight);

        Color outline = new(28, 25, 48);
        Color steel = new(107, 113, 155);
        Color highlight = new(178, 193, 225);
        Color blue = new(45, 173, 230);
        Color pink = new(237, 92, 187);
        Color purple = new(158, 86, 205);
        Color orange = new(248, 159, 65);

        // Converter: a compact violet press with a bright egg window.
        DrawMachineBody(pixels, tileWidth * columns, 0, outline, steel, highlight);
        Fill(pixels, tileWidth * columns, 5, 12, 6, 8, purple);
        Fill(pixels, tileWidth * columns, 7, 14, 2, 4, new Color(180, 241, 255));
        Fill(pixels, tileWidth * columns, 5, 22, 6, 2, orange);

        // Breeder: blue and pink chambers under a heart marker.
        int breederX = tileWidth;
        DrawMachineBody(pixels, tileWidth * columns, breederX, outline, steel, highlight);
        Fill(pixels, tileWidth * columns, breederX + 4, 12, 3, 8, blue);
        Fill(pixels, tileWidth * columns, breederX + 9, 12, 3, 8, pink);
        Fill(pixels, tileWidth * columns, breederX + 6, 7, 4, 3, pink);
        Set(pixels, tileWidth * columns, breederX + 7, 6, pink);
        Set(pixels, tileWidth * columns, breederX + 9, 6, pink);
        Fill(pixels, tileWidth * columns, breederX + 6, 10, 4, 1, orange);

        // Incubator: a single glowing violet egg chamber.
        int incubatorX = tileWidth * 2;
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
