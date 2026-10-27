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
    private const string RanchSlimeTexture = "Mods/Taum.SlimeRanching/Animals/RanchSlime";

    public static void LoadAssets(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo(RanchSlimeTexture))
            return;

        e.LoadFrom(() => CreateSpriteSheet(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
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

    private static Texture2D CreateSpriteSheet(GraphicsDevice graphicsDevice)
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
        Color greenTint = new(145, 255, 120);

        for (int row = 0; row < rows; row++)
        for (int frame = 0; frame < columns; frame++)
        {
            int sourceFrame = frame;
            int destinationX = frame * frameWidth;
            int destinationY = row * frameHeight;
            CopyTintedFrame(vanillaPixels, vanillaTexture.Width, pixels, texture.Width,
                new Rectangle(sourceFrame * frameWidth, 0, frameWidth, frameHeight),
                destinationX, destinationY, greenTint);

            // Vanilla eye frame used by a calm slime looking toward the player.
            // The source eye art is white and is intentionally not color-tinted.
            CopyFrame(vanillaPixels, vanillaTexture.Width, pixels, texture.Width,
                new Rectangle(32, 120, frameWidth, frameHeight), destinationX, destinationY);
        }

        texture.SetData(pixels);
        return texture;
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
