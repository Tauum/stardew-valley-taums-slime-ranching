using Microsoft.Xna.Framework;
using System.Text.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using StardewValley.Objects;

namespace taums_slime_ranching.Services;

internal static class BuildingContent
{
    public const string SlimeDaycareBuildingId = "Taum.SlimeRanching_SlimeDaycare";
    public const string SlimeDaycareUpgrade1Id = "Taum.SlimeRanching_SlimeDaycare_Upgrade1";
    public const string SlimeDaycareUpgrade2Id = "Taum.SlimeRanching_SlimeDaycare_Upgrade2";
    public const string SlimeDaycareUpgrade3Id = "Taum.SlimeRanching_SlimeDaycare_Upgrade3";

    private const string VanillaDeluxeBarnBuildingId = "Deluxe Barn";
    private static readonly HashSet<string> SlimeDaycareIds = new(StringComparer.Ordinal)
    {
        SlimeDaycareBuildingId,
        SlimeDaycareUpgrade1Id,
        SlimeDaycareUpgrade2Id,
        SlimeDaycareUpgrade3Id
    };

    public static bool IsSlimeDaycare(string? buildingId) =>
        buildingId is not null && SlimeDaycareIds.Contains(buildingId);

    public static void EditAsset(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Buildings"))
            return;

        e.Edit(asset =>
        {
            var buildings = asset.AsDictionary<string, BuildingData>().Data;
            if (!buildings.TryGetValue(VanillaDeluxeBarnBuildingId, out BuildingData? deluxeBarn))
                return;

            var options = new JsonSerializerOptions { IncludeFields = true };
            BuildingData daycare = Clone(deluxeBarn, options);
            daycare.Name = "Slime Daycare";
            daycare.NameForGeneralType = "Slime Daycare";
            daycare.Description = "A safe animal house for friendly Ranch Slimes.";
            daycare.BuildingType = "";
            daycare.BuildingToUpgrade = null;
            daycare.IndoorMap = "FarmHouse1";
            daycare.IndoorMapType = "StardewValley.AnimalHouse";
            daycare.BuildDays = 0;
            daycare.BuildCost = 0;
            daycare.BuildMaterials = new();
            daycare.MaxOccupants = 10;
            daycare.HayCapacity = 0;
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
            upgrade1.IndoorMap = "FarmHouse2";
            upgrade1.MaxOccupants = 20;
            upgrade1.BuildCost = 10_000;
            upgrade1.BuildMaterials = new List<BuildingMaterial> { new() { ItemId = "(O)388", Amount = 450 } };
            upgrade1.ValidOccupantTypes = new List<string> { SlimeDaycareBuildingId };
            buildings[SlimeDaycareUpgrade1Id] = upgrade1;

            BuildingData upgrade2 = Clone(daycare, options);
            upgrade2.Name = "Slime Daycare - Upgrade 2";
            upgrade2.NameForGeneralType = "Slime Daycare";
            upgrade2.Description = "The largest interior, with room for even more Ranch Slimes.";
            upgrade2.BuildingToUpgrade = SlimeDaycareUpgrade1Id;
            upgrade2.IndoorMap = "FarmHouse2";
            upgrade2.MaxOccupants = 20;
            upgrade2.BuildCost = 65_000;
            upgrade2.BuildMaterials = new List<BuildingMaterial> { new() { ItemId = "(O)709", Amount = 100 } };
            upgrade2.ValidOccupantTypes = new List<string> { SlimeDaycareBuildingId, SlimeDaycareUpgrade1Id };
            buildings[SlimeDaycareUpgrade2Id] = upgrade2;

            BuildingData upgrade3 = Clone(daycare, options);
            upgrade3.Name = "Slime Daycare - Cellar";
            upgrade3.NameForGeneralType = "Slime Daycare";
            upgrade3.Description = "Adds the farmhouse cellar beneath the Daycare.";
            upgrade3.BuildingToUpgrade = SlimeDaycareUpgrade2Id;
            upgrade3.IndoorMap = "FarmHouse2";
            upgrade3.MaxOccupants = 30;
            upgrade3.BuildCost = 100_000;
            upgrade3.BuildMaterials = new();
            upgrade3.ValidOccupantTypes = new List<string>
            {
                SlimeDaycareBuildingId,
                SlimeDaycareUpgrade1Id,
                SlimeDaycareUpgrade2Id
            };
            buildings[SlimeDaycareUpgrade3Id] = upgrade3;
        });
    }

    public static void RefreshLoadedDaycareInteriors(IMonitor monitor)
    {
        foreach (Building building in Game1.getFarm().buildings)
        {
            if (!IsSlimeDaycare(building.buildingType.Value))
                continue;

            BuildingData? data = building.GetData();
            if (data is null || string.IsNullOrWhiteSpace(data.IndoorMap))
                continue;

            building.ReloadBuildingData();
            if (building.GetIndoors() is not AnimalHouse house)
                continue;

            // Old saves can retain a Barn map and its baked-in hay hopper after
            // the building data changes. Reload the configured map on save load.
            house.mapPath.Value = "Maps\\" + data.IndoorMap;
            house.reloadMap();
            house.MakeMapModifications(force: true);
            house.animalLimit.Value = data.MaxOccupants;
            house.piecesOfHay.Value = 0;

            int removed = RemoveFarmhouseOnlyFixtures(house);
            if (removed > 0)
                monitor.Log($"Removed farmhouse kitchen appliances, fireplace, hay, vanilla incubator, and cellar casks from the Slime Daycare ({removed} saved items or map tiles).", LogLevel.Info);
        }
    }

    internal static int RemoveFarmhouseOnlyFixtures(AnimalHouse house)
    {
        int removed = 0;
        foreach (var pair in house.objects.Pairs.ToArray())
        {
            StardewValley.Object obj = pair.Value;
            bool isHay = obj.QualifiedItemId == "(O)178"
                || obj.Name.Contains("Hay", StringComparison.OrdinalIgnoreCase)
                || obj.Name.Contains("Hopper", StringComparison.OrdinalIgnoreCase);
            bool isVanillaIncubator = obj.GetMachineData()?.IsIncubator == true;
            bool isKitchenFixture = obj is Chest chest && chest.fridge.Value
                || obj.Name.Contains("Stove", StringComparison.OrdinalIgnoreCase)
                || obj.Name.Contains("Oven", StringComparison.OrdinalIgnoreCase)
                || obj.Name.Contains("Refrigerator", StringComparison.OrdinalIgnoreCase);
            if (!isHay && !isVanillaIncubator && !isKitchenFixture)
                continue;

            house.objects.Remove(pair.Key);
            removed++;
        }

        removed += RemoveDaycareHomeFurniture(house);
        removed += RemoveKitchenAndFridgeMapTiles(house);
        removed += DaycareRenovations.RemoveCellarKegs(house);
        return removed;
    }

    internal static int RemoveDaycareHomeFurniture(AnimalHouse house)
    {
        int removed = 0;
        foreach (Furniture furniture in house.furniture.ToArray())
        {
            string id = furniture.QualifiedItemId;
            string name = furniture.Name;
            bool isHayHopper = name.Contains("Hay", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Hopper", StringComparison.OrdinalIgnoreCase);
            bool isFireplace = id is "(F)1792" or "(F)1794"
                || name.Contains("Fireplace", StringComparison.OrdinalIgnoreCase);
            bool isKitchenMachine = name.Contains("Stove", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Oven", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Refrigerator", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Kitchen", StringComparison.OrdinalIgnoreCase);
            if (!isFireplace && !isKitchenMachine && !isHayHopper)
                continue;

            house.furniture.Remove(furniture);
            removed++;
        }
        return removed;
    }

    private static int RemoveKitchenAndFridgeMapTiles(AnimalHouse house)
    {
        int removed = 0;
        var buildings = house.Map.GetLayer("Buildings");
        if (buildings is null)
            return 0;

        List<Point> fridgeTiles = new();
        List<Point> kitchenActionTiles = new();
        for (int y = 0; y < buildings.LayerHeight; y++)
        for (int x = 0; x < buildings.LayerWidth; x++)
        {
            if (buildings.Tiles[x, y] is { TileIndex: 173 } fridgeTile
                && string.Equals(fridgeTile.TileSheet.Id, "untitled tile sheet", StringComparison.OrdinalIgnoreCase))
                fridgeTiles.Add(new Point(x, y));

            foreach (string layerName in new[] { "Back", "Buildings", "Buildings2", "Front" })
            {
                var layer = house.Map.GetLayer(layerName);
                var tile = layer?.Tiles[x, y];
                if (tile is null || !tile.Properties.TryGetValue("Action", out var action))
                    continue;
                if (string.Equals(action, "Kitchen", StringComparison.OrdinalIgnoreCase))
                    kitchenActionTiles.Add(new Point(x, y));
            }
        }

        foreach (Point point in fridgeTiles)
        {
            RemoveTile(point.X, point.Y - 1, "Buildings");
            RemoveTile(point.X, point.Y, "Buildings");
        }

        foreach (Point point in kitchenActionTiles.Distinct())
        {
            // The kitchen action is on the stove. Clear its 3x2 appliance art
            // from foreground layers while preserving the floor and walls.
            for (int x = point.X - 1; x <= point.X + 1; x++)
            for (int y = point.Y - 1; y <= point.Y; y++)
                foreach (string layerName in new[] { "Buildings", "Buildings2", "Front" })
                    RemoveTile(x, y, layerName);
        }

        return removed;

        void RemoveTile(int x, int y, string layerName)
        {
            var layer = house.Map.GetLayer(layerName);
            if (layer is null || x < 0 || y < 0 || x >= layer.LayerWidth || y >= layer.LayerHeight || layer.Tiles[x, y] is null)
                return;
            house.removeTile(x, y, layerName);
            removed++;
        }
    }

    private static BuildingData Clone(BuildingData source, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<BuildingData>(JsonSerializer.Serialize(source, options), options)!;

}
