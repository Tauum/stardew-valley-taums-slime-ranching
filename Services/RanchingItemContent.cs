using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;

namespace taums_slime_ranching.Services;

/// <summary>Defines the Slime Breeder and Slime Incubator items and the machine icon sheet.</summary>
internal static class RanchingItemContent
{
    private const string BreederItemId = "Taum.SlimeRanching_SlimeBreeder";
    private const string IncubatorItemId = "Taum.SlimeRanching_SlimeIncubator";
    public const string IconTextureName = "Mods/Taum.SlimeRanching/Machines/ModMachineIcons";

    public const string BreederQualifiedId = "(BC)" + BreederItemId;
    public const string IncubatorQualifiedId = "(BC)" + IncubatorItemId;

    public static void LoadAssets(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(IconTextureName))
            e.LoadFrom(() => CreateMachineIcons(Game1.graphics.GraphicsDevice), AssetLoadPriority.Exclusive);
    }

    public static void EditAsset(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
            return;

        e.Edit(asset =>
        {
            var craftables = asset.AsDictionary<string, BigCraftableData>().Data;
            craftables.TryAdd(BreederItemId, new BigCraftableData
            {
                Name = "Slime Breeder",
                DisplayName = "Slime Breeder",
                Description = "Select two Domesticated Slimes for breeding.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Texture = IconTextureName,
                SpriteIndex = 0
            });

            craftables.TryAdd(IncubatorItemId, new BigCraftableData
            {
                Name = "Slime Incubator",
                DisplayName = "Slime Incubator",
                Description = "Hatches a genome-specific Domesticated Slime Egg inside the Slime Daycare.",
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
                Texture = IconTextureName,
                SpriteIndex = 1
            });
        });
    }

    private static Texture2D CreateMachineIcons(GraphicsDevice graphicsDevice)
    {
        const int tileWidth = 16;
        const int tileHeight = 32;
        const int columns = 5;
        var pixels = new Color[tileWidth * columns * tileHeight];
        var texture = new Texture2D(graphicsDevice, tileWidth * columns, tileHeight);
        string[] machineAssets =
        {
            MachineArtContent.BreederTextureName,
            MachineArtContent.IncubatorTextureName,
            MachineArtContent.ResearchLabTextureName,
            MachineArtContent.MutagenicPoolTextureName,
            MachineArtContent.SlimePrinterTextureName
        };

        for (int index = 0; index < machineAssets.Length; index++)
        {
            Texture2D machineArt = MachineArtContent.GetTexture(machineAssets[index]);
            DrawMachineArtIcon(pixels, tileWidth * columns, index * tileWidth, machineArt);
        }

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
}
