using System.Text.Json;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Buildings;

namespace taums_slime_ranching.Services;

internal static class BuildingContent
{
    private const int PixelsPerTile = 16;
    private const int DaycareTextureHeight = 160;
    private const int DaycareBaseTextureWidth = 160;

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

    public static void EditAsset(AssetRequestedEventArgs e, IModHelper helper)
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
            daycare.Texture = GetTextureKey(helper, "SlimeDaycareBase_GameTexture.png");
            // The reference footprint is ten tiles wide; later tiers each add
            // one tile. These dimensions match the directly scaled source art.
            daycare.Size = new Point(DaycareBaseTextureWidth / PixelsPerTile, barn.Size.Y);
            daycare.SourceRect = new Rectangle(0, 0, daycare.Size.X * PixelsPerTile, DaycareTextureHeight);
            // Visually align the building body with the marked footprint without
            // translating/cropping its roof in the source texture.
            daycare.DrawOffset = new Vector2(3f, 8f);
            // Explicitly allow this custom animal's House ID. Some animal
            // placement paths only consult ValidOccupantTypes for modded types.
            daycare.ValidOccupantTypes = new List<string> { SlimeDaycareBuildingId };
            daycare.AllowAnimalPregnancy = false;
            buildings[SlimeDaycareBuildingId] = daycare;

            BuildingData upgrade1 = Clone(daycare, options);
            upgrade1.Name = "Slime Daycare - Upgrade 1";
            upgrade1.NameForGeneralType = "Slime Daycare";
            upgrade1.Description = "A larger daycare with a blue roof and room for more Ranch Slimes.";
            upgrade1.BuildingToUpgrade = SlimeDaycareBuildingId;
            upgrade1.MaxOccupants = 20;
            upgrade1.Size = new Point(daycare.Size.X + 1, daycare.Size.Y);
            upgrade1.SourceRect = new Rectangle(0, 0, upgrade1.Size.X * PixelsPerTile, DaycareTextureHeight);
            upgrade1.Texture = GetTextureKey(helper, "SlimeDaycareUpgrade1_GameTexture.png");
            upgrade1.ValidOccupantTypes = new List<string> { SlimeDaycareBuildingId };
            buildings[SlimeDaycareUpgrade1Id] = upgrade1;

            BuildingData upgrade2 = Clone(daycare, options);
            upgrade2.Name = "Slime Daycare - Upgrade 2";
            upgrade2.NameForGeneralType = "Slime Daycare";
            upgrade2.Description = "The grand daycare with a purple roof and room for even more Ranch Slimes.";
            upgrade2.BuildingToUpgrade = SlimeDaycareUpgrade1Id;
            upgrade2.MaxOccupants = 30;
            upgrade2.Size = new Point(upgrade1.Size.X + 1, upgrade1.Size.Y);
            upgrade2.SourceRect = new Rectangle(0, 0, upgrade2.Size.X * PixelsPerTile, DaycareTextureHeight);
            upgrade2.Texture = GetTextureKey(helper, "SlimeDaycareUpgrade2_GameTexture.png");
            upgrade2.ValidOccupantTypes = new List<string> { SlimeDaycareBuildingId, SlimeDaycareUpgrade1Id };
            buildings[SlimeDaycareUpgrade2Id] = upgrade2;
        });
    }

    private static BuildingData Clone(BuildingData source, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<BuildingData>(JsonSerializer.Serialize(source, options), options)!;

    private static string GetTextureKey(IModHelper helper, string fileName) =>
        helper.ModContent.GetInternalAssetName($"assets/Buildings/{fileName}").BaseName;
}
