using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Buildings;

namespace taums_slime_ranching.Services;

internal static class BuildingContent
{
    private const int PixelsPerTile = 16;
    private const int DaycareFootprintWidth = 8;
    private const int DaycareFootprintHeight = 5;
    private const int DaycareTextureWidth = DaycareFootprintWidth * PixelsPerTile;
    private const int DaycareTextureHeight = DaycareFootprintHeight * PixelsPerTile;
    private const int DesignCellWidth = 384;
    private const string ExteriorTextureAssetPrefix = "Mods/Taum.SlimeRanching/Buildings/SlimeDaycare/";
    private static readonly int[] DesignRowBounds = { 0, 341, 683, 1024 };
    private static readonly string[] ExteriorDesignIds =
    {
        "RedRanch", "Forest", "Harbor", "Harvest", "Moonlit", "Winter", "Coastal", "Stone",
        "Carnival", "Woodland", "Bloom", "Desert"
    };
    private static readonly string[] ExteriorDesignNames =
    {
        "Red Ranch", "Forest", "Harbor", "Harvest", "Moonlit", "Winter", "Coastal", "Stone",
        "Carnival", "Woodland", "Bloom", "Desert"
    };
    private static Color[]? designSheetPixels;
    private static int designSheetWidth;
    private static int designSheetHeight;

    public const string SlimeDaycareBuildingId = "Taum.SlimeRanching_SlimeDaycare";
    public const string SlimeDaycareUpgrade1Id = "Taum.SlimeRanching_SlimeDaycare_Upgrade1";
    public const string SlimeDaycareUpgrade2Id = "Taum.SlimeRanching_SlimeDaycare_Upgrade2";

    private const string VanillaBarnBuildingId = "Barn";
    private static readonly HashSet<string> SlimeDaycareIds = new(StringComparer.Ordinal)
    {
        SlimeDaycareBuildingId,
        SlimeDaycareUpgrade1Id,
        SlimeDaycareUpgrade2Id
    };

    public static bool IsSlimeDaycare(string? buildingId) =>
        buildingId is not null && SlimeDaycareIds.Contains(buildingId);

    public static string[] GetExteriorDesignIds() => ExteriorDesignIds.ToArray();

    public static string GetExteriorDesignName(string designId)
    {
        int index = Array.IndexOf(ExteriorDesignIds, designId);
        return index >= 0 ? ExteriorDesignNames[index] : ExteriorDesignNames[0];
    }

    public static void NormalizeConfig(ModConfig config)
    {
        if (!ExteriorDesignIds.Contains(config.SlimeDaycareExteriorDesign, StringComparer.Ordinal))
            config.SlimeDaycareExteriorDesign = ExteriorDesignIds[0];
    }

    public static string GetExteriorTextureName(string designId)
    {
        int index = Array.IndexOf(ExteriorDesignIds, designId);
        if (index < 0)
            index = 0;
        return ExteriorTextureAssetPrefix + ExteriorDesignIds[index];
    }

    public static void LoadAssets(AssetRequestedEventArgs e, IModHelper helper)
    {
        for (int i = 0; i < ExteriorDesignIds.Length; i++)
        {
            string designId = ExteriorDesignIds[i];
            if (e.NameWithoutLocale.IsEquivalentTo(GetExteriorTextureName(designId)))
            {
                int designIndex = i;
                e.LoadFrom(() => CreateExteriorTexture(Game1.graphics.GraphicsDevice, helper, designIndex), AssetLoadPriority.Exclusive);
                return;
            }
        }
    }

    public static void EditAsset(AssetRequestedEventArgs e, IModHelper helper, ModConfig config)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Buildings"))
            return;

        e.Edit(asset =>
        {
            var buildings = asset.AsDictionary<string, BuildingData>().Data;
            if (!buildings.TryGetValue(VanillaBarnBuildingId, out BuildingData? barn))
                return;

            var options = new JsonSerializerOptions { IncludeFields = true };
            BuildingData daycare = Clone(barn, options);
            daycare.Name = "Slime Daycare";
            daycare.NameForGeneralType = "Slime Daycare";
            daycare.Description = "A safe animal house for friendly Ranch Slimes.";
            daycare.BuildingType = "";
            daycare.BuildingToUpgrade = null;
            daycare.IndoorMap = "Barn3";
            daycare.IndoorMapType = "StardewValley.AnimalHouse";
            daycare.BuildDays = 0;
            daycare.BuildCost = 0;
            daycare.BuildMaterials = new();
            daycare.MaxOccupants = 12;
            string selectedTexture = GetExteriorTextureName(config.SlimeDaycareExteriorDesign);
            daycare.Texture = selectedTexture;
            daycare.Size = new Point(DaycareFootprintWidth, DaycareFootprintHeight);
            daycare.SourceRect = new Rectangle(0, 0, daycare.Size.X * PixelsPerTile, DaycareTextureHeight);
            // Use the full 7x4 tile footprint as the source art bounds.
            daycare.DrawOffset = new Vector2(3f, 8f);
            // Explicitly allow this custom animal's House ID. Some animal
            // placement paths only consult ValidOccupantTypes for modded types.
            daycare.ValidOccupantTypes = new List<string> { SlimeDaycareBuildingId };
            daycare.AllowAnimalPregnancy = false;
            buildings[SlimeDaycareBuildingId] = daycare;

            BuildingData upgrade1 = Clone(daycare, options);
            upgrade1.Name = "Slime Daycare - Upgrade 1";
            upgrade1.NameForGeneralType = "Slime Daycare";
            upgrade1.Description = "An expanded interior with room for more Ranch Slimes.";
            upgrade1.BuildingToUpgrade = SlimeDaycareBuildingId;
            upgrade1.MaxOccupants = 20;
            upgrade1.Size = daycare.Size;
            upgrade1.SourceRect = new Rectangle(0, 0, upgrade1.Size.X * PixelsPerTile, DaycareTextureHeight);
            upgrade1.Texture = selectedTexture;
            upgrade1.ValidOccupantTypes = new List<string> { SlimeDaycareBuildingId };
            buildings[SlimeDaycareUpgrade1Id] = upgrade1;

            BuildingData upgrade2 = Clone(daycare, options);
            upgrade2.Name = "Slime Daycare - Upgrade 2";
            upgrade2.NameForGeneralType = "Slime Daycare";
            upgrade2.Description = "The largest interior, with room for even more Ranch Slimes.";
            upgrade2.BuildingToUpgrade = SlimeDaycareUpgrade1Id;
            upgrade2.MaxOccupants = 30;
            upgrade2.Size = daycare.Size;
            upgrade2.SourceRect = new Rectangle(0, 0, upgrade2.Size.X * PixelsPerTile, DaycareTextureHeight);
            upgrade2.Texture = selectedTexture;
            upgrade2.ValidOccupantTypes = new List<string> { SlimeDaycareBuildingId, SlimeDaycareUpgrade1Id };
            buildings[SlimeDaycareUpgrade2Id] = upgrade2;
        });
    }

    private static Texture2D CreateExteriorTexture(GraphicsDevice graphicsDevice, IModHelper helper, int designIndex)
    {
        Texture2D sheet = helper.ModContent.Load<Texture2D>("assets/Buildings/SlimeDaycareDesigns.png");
        Color[] source = GetTransparentDesignSheetPixels(sheet);
        int column = designIndex % 4;
        int row = designIndex / 4;
        int sourceX = column * DesignCellWidth;
        int sourceY = DesignRowBounds[row];
        int sourceHeight = DesignRowBounds[row + 1] - sourceY;
        Color[] output = new Color[DaycareTextureWidth * DaycareTextureHeight];

        // Use nearest-neighbor scaling to fit the complete 7x4 building source
        // rectangle (112x64 pixels) without texture pixels extending past it.
        for (int y = 0; y < DaycareTextureHeight; y++)
        for (int x = 0; x < DaycareTextureWidth; x++)
        {
            int sampleX = sourceX + x * DesignCellWidth / DaycareTextureWidth;
            int sampleY = sourceY + y * sourceHeight / DaycareTextureHeight;
            output[y * DaycareTextureWidth + x] = source[sampleY * designSheetWidth + sampleX];
        }

        var texture = new Texture2D(graphicsDevice, DaycareTextureWidth, DaycareTextureHeight);
        texture.SetData(output);
        return texture;
    }

    private static Color[] GetTransparentDesignSheetPixels(Texture2D sheet)
    {
        if (designSheetPixels is not null && designSheetWidth == sheet.Width && designSheetHeight == sheet.Height)
            return designSheetPixels;

        designSheetWidth = sheet.Width;
        designSheetHeight = sheet.Height;
        designSheetPixels = new Color[sheet.Width * sheet.Height];
        sheet.GetData(designSheetPixels);
        Color[] pixels = designSheetPixels;
        var queue = new Queue<int>();
        for (int x = 0; x < sheet.Width; x++)
        {
            EnqueueBackgroundPixel(x);
            EnqueueBackgroundPixel((sheet.Height - 1) * sheet.Width + x);
        }
        for (int y = 1; y < sheet.Height - 1; y++)
        {
            EnqueueBackgroundPixel(y * sheet.Width);
            EnqueueBackgroundPixel(y * sheet.Width + sheet.Width - 1);
        }

        while (queue.TryDequeue(out int index))
        {
            int x = index % sheet.Width;
            int y = index / sheet.Width;
            if (x > 0) EnqueueBackgroundPixel(index - 1);
            if (x + 1 < sheet.Width) EnqueueBackgroundPixel(index + 1);
            if (y > 0) EnqueueBackgroundPixel(index - sheet.Width);
            if (y + 1 < sheet.Height) EnqueueBackgroundPixel(index + sheet.Width);
        }

        return designSheetPixels!;

        void EnqueueBackgroundPixel(int index)
        {
            Color pixel = pixels[index];
            if (pixel.A == 0 || pixel.R > 2 || pixel.G > 2 || pixel.B > 2)
                return;
            pixels[index] = Color.Transparent;
            queue.Enqueue(index);
        }
    }

    private static BuildingData Clone(BuildingData source, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<BuildingData>(JsonSerializer.Serialize(source, options), options)!;

}
