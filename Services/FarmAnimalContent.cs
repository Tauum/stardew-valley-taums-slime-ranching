using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.FarmAnimals;

namespace taums_slime_ranching.Services;

internal static class FarmAnimalContent
{
    public const string RanchSlimeTypeId = "Taum.SlimeRanching_RanchSlime";
    public const int RanchSlimeDaysToMature = 28;
    private const string RanchSlimeTexturePrefix = "Mods/Taum.SlimeRanching/Animals/RanchSlime_";
    private const string RanchSlimeTexture = RanchSlimeTexturePrefix + "Green_Normal";
    private static Color[]? vanillaSlimePixels;
    private static int vanillaSlimePixelWidth;

    private static readonly (string Name, Color Reference, Color Tint)[] ColorVariants =
    {
        ("Red", new Color(255, 0, 0), new Color(255, 92, 92)),
        ("Orange", new Color(255, 149, 0), new Color(255, 157, 76)),
        ("Yellow", new Color(255, 214, 10), new Color(255, 225, 90)),
        ("Green", new Color(0, 255, 0), new Color(145, 255, 120)),
        ("Blue", new Color(10, 132, 255), new Color(92, 177, 255)),
        ("Purple", new Color(191, 90, 242), new Color(192, 128, 255)),
        ("Pink", new Color(255, 105, 180), new Color(255, 137, 199))
    };

    public static void LoadAssets(AssetRequestedEventArgs e)
    {
        string requestedName = e.NameWithoutLocale.BaseName;
        if (!requestedName.StartsWith(RanchSlimeTexturePrefix, StringComparison.Ordinal))
            return;

        string[] parts = requestedName[RanchSlimeTexturePrefix.Length..].Split('_');
        if (parts.Length is < 2 or > 3 || !TryGetColorVariant(parts[0], out var variant))
            return;

        float scale = GetSizeScale(parts[1]);
        bool isShiny = parts.Length == 3 && parts[2].Equals("Shiny", StringComparison.OrdinalIgnoreCase);
        if (scale <= 0f || (parts.Length == 3 && !isShiny))
            return;

        e.LoadFrom(() => CreateSpriteSheet(Game1.graphics.GraphicsDevice, variant.Tint, scale, isShiny), AssetLoadPriority.Exclusive);
    }

    public static void ApplyAppearance(FarmAnimal animal, string? visualColor, string? size = null)
        => ApplyAppearance(animal, visualColor, size, isShiny: false);

    public static void ApplyAppearance(FarmAnimal animal, string? visualColor, string? size, bool isShiny)
    {
        string texture = GetTextureName(visualColor, size, isShiny);
        animal.Sprite.LoadTexture(texture, syncTextureName: false);
    }

    public static string GetTextureName(string? visualColor, string? size = null)
    {
        return RanchSlimeTexturePrefix + GetClosestVariant(visualColor).Name + "_" + GetCanonicalSize(size);
    }

    public static string GetTextureName(string? visualColor, string? size, bool isShiny) =>
        GetTextureName(visualColor, size) + (isShiny ? "_Shiny" : "");

    public static Color GetVisualTint(string? visualColor) => GetClosestVariant(visualColor).Tint;

    public static string GetClosestVariantName(string? visualColor) => GetClosestVariant(visualColor).Name;

    public static string GetReferenceHex(string variantName)
    {
        var variant = ColorVariants.FirstOrDefault(value => value.Name.Equals(variantName, StringComparison.OrdinalIgnoreCase));
        return variant.Name is null ? "#00FF00" : $"#{variant.Reference.R:X2}{variant.Reference.G:X2}{variant.Reference.B:X2}";
    }

    public static Color GetTintForVariantName(string name) =>
        ColorVariants.FirstOrDefault(variant => variant.Name == name).Tint;

    private static (string Name, Color Reference, Color Tint) GetClosestVariant(string? visualColor)
    {
        Color requested = ParseHexColor(visualColor);
        return ColorVariants.OrderBy(variant => ColorDistanceSquared(requested, variant.Reference)).First();
    }

    public static void EditAsset(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/FarmAnimals"))
            return;

        e.Edit(asset =>
        {
            var animals = asset.AsDictionary<string, FarmAnimalData>().Data;
            if (!animals.ContainsKey(RanchSlimeTypeId))
            {
                animals[RanchSlimeTypeId] = new FarmAnimalData
                {
                    DisplayName = "Ranch Slime",
                    House = BuildingContent.SlimeDaycareBuildingId,
                    PurchasePrice = 1000,
                    SellPrice = 0,
                    DaysToMature = RanchSlimeDaysToMature,
                    CanGetPregnant = false,
                    DaysToProduce = 0,
                    GrassEatAmount = 0,
                    HappinessDrain = 0,
                    Texture = RanchSlimeTexture,
                    SpriteWidth = 40,
                    SpriteHeight = 60,
                    UseFlippedRightForLeft = true,
                    CanEatGoldenCrackers = false
                };
            }
        });
    }

    private static Texture2D CreateSpriteSheet(GraphicsDevice graphicsDevice, Color slimeTint, float sizeScale, bool isShiny)
    {
        const int sourceFrameWidth = 16;
        const int sourceFrameHeight = 24;
        const int frameWidth = 40;
        const int frameHeight = 60;
        const int columns = 4;
        const int rows = 5;
        Color[] vanillaPixels = GetVanillaSlimePixels(out int vanillaTextureWidth);

        // Recompose each vanilla 16x24 slime frame, then nearest-neighbor scale its
        // opaque pixels into a larger transparent 40x60 farm-animal frame. The
        // normal-size variant keeps the original pixel dimensions and is bottom-aligned.
        int sourceSheetWidth = sourceFrameWidth * columns;
        int sourceSheetHeight = sourceFrameHeight * rows;
        var sourcePixels = new Color[sourceSheetWidth * sourceSheetHeight];
        for (int row = 0; row < rows; row++)
        for (int frame = 0; frame < columns; frame++)
        {
            int sourceFrame = frame;
            int destinationX = frame * sourceFrameWidth;
            int destinationY = row * sourceFrameHeight;
            CopyTintedFrame(vanillaPixels, vanillaTextureWidth, sourcePixels, sourceSheetWidth,
                new Rectangle(sourceFrame * sourceFrameWidth, 0, sourceFrameWidth, sourceFrameHeight),
                destinationX, destinationY, slimeTint);

            // Vanilla eye frame used by a calm slime looking toward the player.
            // The source eye art is white and is intentionally not color-tinted.
            CopyFrame(vanillaPixels, vanillaTextureWidth, sourcePixels, sourceSheetWidth,
                new Rectangle(32, 120, sourceFrameWidth, sourceFrameHeight), destinationX, destinationY);
        }

        var pixels = new Color[frameWidth * columns * frameHeight * rows];
        int scaledWidth = Math.Clamp((int)MathF.Round(sourceFrameWidth * sizeScale), 1, frameWidth);
        int scaledHeight = Math.Clamp((int)MathF.Round(sourceFrameHeight * sizeScale), 1, frameHeight);
        int offsetX = (frameWidth - scaledWidth) / 2;
        int offsetY = frameHeight - scaledHeight;
        for (int row = 0; row < rows; row++)
        for (int frame = 0; frame < columns; frame++)
        for (int y = 0; y < scaledHeight; y++)
        for (int x = 0; x < scaledWidth; x++)
        {
            int sourceX = Math.Min(sourceFrameWidth - 1, (int)(x / sizeScale));
            int sourceY = Math.Min(sourceFrameHeight - 1, (int)(y / sizeScale));
            Color pixel = sourcePixels[(row * sourceFrameHeight + sourceY) * sourceSheetWidth + frame * sourceFrameWidth + sourceX];
            int targetX = frame * frameWidth + offsetX + x;
            int targetY = row * frameHeight + offsetY + y;
            if (isShiny && pixel.A > 0)
            {
                const float sheen = 0.22f;
                pixel = new Color(
                    (byte)(pixel.R + (255 - pixel.R) * sheen),
                    (byte)(pixel.G + (255 - pixel.G) * sheen),
                    (byte)(pixel.B + (255 - pixel.B) * sheen),
                    pixel.A);
            }
            pixels[targetY * frameWidth * columns + targetX] = pixel;
        }

        var texture = new Texture2D(graphicsDevice, frameWidth * columns, frameHeight * rows);
        texture.SetData(pixels);
        return texture;
    }

    private static bool TryGetColorVariant(string name, out (string Name, Color Reference, Color Tint) variant)
    {
        variant = ColorVariants.FirstOrDefault(color => color.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return !string.IsNullOrEmpty(variant.Name);
    }

    private static string GetCanonicalSize(string? size)
    {
        // Keep existing external records and eggs usable after Tiny is retired.
        if (string.Equals(size, "Tiny", StringComparison.OrdinalIgnoreCase))
            return "Small";

        string[] sizes = { "Small", "Normal", "Large", "Giant" };
        return sizes.FirstOrDefault(value => value.Equals(size, StringComparison.OrdinalIgnoreCase)) ?? "Normal";
    }

    private static float GetSizeScale(string size) => GetCanonicalSize(size) switch
    {
        "Small" => 0.82f,
        "Large" => 1.18f,
        "Giant" => 2.5f,
        _ => size.Equals("Normal", StringComparison.OrdinalIgnoreCase) ? 1f : 0f
    };

    private static Color[] GetVanillaSlimePixels(out int sourceWidth)
    {
        Texture2D source = Game1.content.Load<Texture2D>("Characters/Monsters/Green Slime");
        sourceWidth = source.Width;
        if (vanillaSlimePixels is null || vanillaSlimePixelWidth != source.Width
            || vanillaSlimePixels.Length != source.Width * source.Height)
        {
            vanillaSlimePixels = new Color[source.Width * source.Height];
            source.GetData(vanillaSlimePixels);
            vanillaSlimePixelWidth = source.Width;
        }

        return vanillaSlimePixels;
    }

    private static Color ParseHexColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ColorVariants.First(variant => variant.Name == "Green").Reference;

        if (string.Equals(value, "Mint", StringComparison.OrdinalIgnoreCase))
            return ColorVariants.First(variant => variant.Name == "Green").Reference;
        if (string.Equals(value, "Violet", StringComparison.OrdinalIgnoreCase))
            return ColorVariants.First(variant => variant.Name == "Purple").Reference;

        string hex = value.TrimStart('#');
        if (hex.Length == 6
            && byte.TryParse(hex[..2], System.Globalization.NumberStyles.HexNumber, null, out byte red)
            && byte.TryParse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out byte green)
            && byte.TryParse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out byte blue))
            return new Color(red, green, blue);

        return ColorVariants.First(variant => variant.Name == "Green").Reference;
    }

    private static int ColorDistanceSquared(Color a, Color b)
    {
        int red = a.R - b.R;
        int green = a.G - b.G;
        int blue = a.B - b.B;
        return red * red + green * green + blue * blue;
    }

    private static void CopyTintedFrame(Color[] source, int sourceWidth, Color[] destination, int destinationWidth,
        Rectangle sourceRect, int destinationX, int destinationY, Color tint)
    {
        for (int y = 0; y < sourceRect.Height; y++)
        for (int x = 0; x < sourceRect.Width; x++)
        {
            Color sourcePixel = source[(sourceRect.Y + y) * sourceWidth + sourceRect.X + x];
            Color pixel = new(
                (byte)(sourcePixel.R * tint.R / 255),
                (byte)(sourcePixel.G * tint.G / 255),
                (byte)(sourcePixel.B * tint.B / 255),
                sourcePixel.A);
            destination[(destinationY + y) * destinationWidth + destinationX + x] = pixel;
        }
    }

    private static void CopyFrame(Color[] source, int sourceWidth, Color[] destination, int destinationWidth,
        Rectangle sourceRect, int destinationX, int destinationY)
    {
        for (int y = 0; y < sourceRect.Height; y++)
        for (int x = 0; x < sourceRect.Width; x++)
        {
            Color pixel = source[(sourceRect.Y + y) * sourceWidth + sourceRect.X + x];
            if (pixel.A > 0)
                destination[(destinationY + y) * destinationWidth + destinationX + x] = pixel;
        }
    }
}
