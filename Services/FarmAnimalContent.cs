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
    public const int RanchSlimeDaysToMature = 3;
    private const string RanchSlimeTexturePrefix = "Mods/Taum.SlimeRanching/Animals/RanchSlime_";
    private const string RanchSlimeTexture = RanchSlimeTexturePrefix + "Green";

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

        string colorName = requestedName[RanchSlimeTexturePrefix.Length..];
        (string Name, Color Reference, Color Tint)? variant = ColorVariants.FirstOrDefault(color => color.Name == colorName);
        if (variant is null)
            return;

        e.LoadFrom(() => CreateSpriteSheet(Game1.graphics.GraphicsDevice, variant.Value.Tint), AssetLoadPriority.Exclusive);
    }

    public static void ApplyAppearance(FarmAnimal animal, string? visualColor)
    {
        string texture = GetTextureName(visualColor);
        animal.Sprite.LoadTexture(texture, syncTextureName: false);
    }

    public static string GetTextureName(string? visualColor)
    {
        return RanchSlimeTexturePrefix + GetClosestVariant(visualColor).Name;
    }

    public static Color GetVisualTint(string? visualColor) => GetClosestVariant(visualColor).Tint;

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
                    SpriteWidth = 16,
                    SpriteHeight = 24,
                    UseFlippedRightForLeft = true,
                    CanEatGoldenCrackers = false
                };
            }
        });
    }

    private static Texture2D CreateSpriteSheet(GraphicsDevice graphicsDevice, Color slimeTint)
    {
        const int frameWidth = 16;
        const int frameHeight = 24;
        const int columns = 4;
        const int rows = 5;
        Texture2D vanillaTexture = Game1.content.Load<Texture2D>("Characters/Monsters/Green Slime");
        var vanillaPixels = new Color[vanillaTexture.Width * vanillaTexture.Height];
        vanillaTexture.GetData(vanillaPixels);

        // Stardew draws GreenSlime from a 16x24 greyscale body frame and overlays
        // its face from the same vanilla sheet. Recompose those exact pixels into
        // the FarmAnimal 4-column layout, using the vanilla green tint.
        var texture = new Texture2D(graphicsDevice, frameWidth * columns, frameHeight * rows);
        var pixels = new Color[texture.Width * texture.Height];
        for (int row = 0; row < rows; row++)
        for (int frame = 0; frame < columns; frame++)
        {
            int sourceFrame = frame;
            int destinationX = frame * frameWidth;
            int destinationY = row * frameHeight;
            CopyTintedFrame(vanillaPixels, vanillaTexture.Width, pixels, texture.Width,
                new Rectangle(sourceFrame * frameWidth, 0, frameWidth, frameHeight),
                destinationX, destinationY, slimeTint);

            // Vanilla eye frame used by a calm slime looking toward the player.
            // The source eye art is white and is intentionally not color-tinted.
            CopyFrame(vanillaPixels, vanillaTexture.Width, pixels, texture.Width,
                new Rectangle(32, 120, frameWidth, frameHeight), destinationX, destinationY);
        }

        texture.SetData(pixels);
        return texture;
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
                sourcePixel.A == 0 ? (byte)0 : (byte)(sourcePixel.A + (255 - sourcePixel.A) / 5));
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
