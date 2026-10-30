using System.Globalization;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;

namespace taums_slime_ranching.Services;

/// <summary>Connects Daycare interiors to Stardew's native AnimalHouse hay feeder.</summary>
internal static class DaycareFeedingService
{
    private const string FeedingSlotProperty = "Taum.SlimeRanching/FeedingTroughSlot";
    private const string FeedingLayoutProperty = "Taum.SlimeRanching/FeedingTroughLayout";

    public static void EnsureConfiguredForFarm(Farm farm, IMonitor monitor)
    {
        foreach (Building building in farm.buildings)
        {
            if (BuildingContent.IsSlimeDaycare(building.buildingType.Value)
                && building.GetIndoors() is AnimalHouse house)
            {
                EnsureConfigured(house, monitor);
            }
        }
    }

    public static int EnsureConfigured(AnimalHouse house, IMonitor? monitor = null)
    {
        if (!BuildingContent.IsSlimeDaycare(house.ParentBuilding?.buildingType.Value))
            return 0;

        xTile.Map? map = house.map;
        if (map is null)
            return 0;

        xTile.Layers.Layer? back = map.GetLayer("Back");
        if (back is null)
            return 0;

        int targetCount = Math.Max(0, house.animalLimit.Value);
        if (targetCount == 0)
            return 0;

        if (TryReadLayout(map, out int savedTargetCount, out List<Vector2> savedSlots)
            && savedTargetCount == targetCount
            && savedSlots.All(slot => HasValidSlot(house, back, slot)))
        {
            map.Properties["AutoFeed"] = "true";
            ConfigureActiveTroughSlots(house, back, savedSlots);
            return savedSlots.Count;
        }

        RemoveOwnedSlots(map, back);

        List<Vector2> slots = FindOpenFloorTiles(house, back)
            .Take(targetCount)
            .ToList();

        foreach (Vector2 slot in slots)
        {
            var tile = back.Tiles[(int)slot.X, (int)slot.Y];
            if (tile is null)
                continue;

            // Keep every reserved slot out of normal placement. Only resident-count
            // slots receive Stardew's native Trough property below.
            tile.Properties[FeedingSlotProperty] = "true";
        }

        ConfigureActiveTroughSlots(house, back, slots);
        map.Properties["AutoFeed"] = "true";
        map.Properties[FeedingLayoutProperty] = SerializeLayout(targetCount, slots);

        if (slots.Count < targetCount)
        {
            monitor?.Log($"The Slime Daycare has room for {slots.Count} native hay trough tile(s), but its capacity is {targetCount}. Check the room layout for obstructed floor tiles.", LogLevel.Warn);
        }
        else
        {
            monitor?.Log($"Configured {slots.Count} native hay trough tile(s) in the Slime Daycare; Stardew will feed them from the farm's hay silos.", LogLevel.Info);
        }

        return slots.Count;
    }

    public static bool IsFeedingTroughTile(AnimalHouse house, int tileX, int tileY)
    {
        var back = house.map?.GetLayer("Back");
        return back is not null
            && tileX >= 0 && tileY >= 0
            && tileX < back.LayerWidth && tileY < back.LayerHeight
            && back.Tiles[tileX, tileY]?.Properties.ContainsKey(FeedingSlotProperty) == true;
    }

    public static IReadOnlyList<Point> GetConfiguredSlots(AnimalHouse house) =>
        house.map is not null && TryReadLayout(house.map, out _, out List<Vector2> slots)
            ? slots.Where(slot => IsFeedingTroughTile(house, (int)slot.X, (int)slot.Y))
                .Select(slot => new Point((int)slot.X, (int)slot.Y))
                .ToArray()
            : Array.Empty<Point>();

    public static int GetActiveTroughSlotCount(AnimalHouse house) =>
        GetConfiguredSlots(house).Count(slot =>
        {
            var back = house.map?.GetLayer("Back");
            return back?.Tiles[slot.X, slot.Y]?.Properties.ContainsKey("Trough") == true;
        });

    public static int GetHayCountOnTroughs(AnimalHouse house)
    {
        if (house.map is null || !TryReadLayout(house.map, out _, out List<Vector2> slots))
            return 0;

        return slots.Count(slot => IsFeedingTroughTile(house, (int)slot.X, (int)slot.Y)
            && house.Objects.TryGetValue(slot, out StardewValley.Object? obj)
            && obj.QualifiedItemId == "(O)178");
    }

    private static IEnumerable<Vector2> FindOpenFloorTiles(AnimalHouse house, xTile.Layers.Layer back)
    {
        int centerX = back.LayerWidth / 2;
        for (int y = 1; y < back.LayerHeight - 1; y++)
        {
            foreach (int x in Enumerable.Range(1, Math.Max(0, back.LayerWidth - 2))
                .OrderBy(candidateX => Math.Abs(candidateX - centerX)))
            {
                Vector2 tile = new(x, y);
                if (back.Tiles[x, y] is null
                    || !house.isTilePassable(tile)
                    || house.doesTileHaveProperty(x, y, "Action", "Back") is not null
                    || house.doesTileHaveProperty(x, y, "Warp", "Back") is not null
                    || house.doesTileHaveProperty(x, y, "Trough", "Back") is not null
                    || house.GetFurnitureAt(tile) is not null
                    || house.terrainFeatures.ContainsKey(tile)
                    || (house.Objects.TryGetValue(tile, out StardewValley.Object? existing)
                        && existing.QualifiedItemId != "(O)178"))
                {
                    continue;
                }

                yield return tile;
            }
        }
    }

    private static bool HasValidSlot(AnimalHouse house, xTile.Layers.Layer back, Vector2 slot)
    {
        int x = (int)slot.X;
        int y = (int)slot.Y;
        Vector2 tile = new(x, y);
        return x >= 0 && y >= 0 && x < back.LayerWidth && y < back.LayerHeight
            && back.Tiles[x, y]?.Properties.ContainsKey(FeedingSlotProperty) == true
            && house.isTilePassable(tile)
            && house.GetFurnitureAt(tile) is null
            && !house.terrainFeatures.ContainsKey(tile)
            && (!house.Objects.TryGetValue(tile, out StardewValley.Object? existing)
                || existing.QualifiedItemId == "(O)178");
    }

    private static void ConfigureActiveTroughSlots(AnimalHouse house, xTile.Layers.Layer back, IReadOnlyList<Vector2> slots)
    {
        int activeSlotCount = Math.Min(house.animalsThatLiveHere.Count, slots.Count);
        for (int index = 0; index < slots.Count; index++)
        {
            Vector2 slot = slots[index];
            var tile = back.Tiles[(int)slot.X, (int)slot.Y];
            if (tile is null)
                continue;

            if (index < activeSlotCount)
                tile.Properties["Trough"] = "true";
            else
                tile.Properties.Remove("Trough");
        }
    }

    private static void RemoveOwnedSlots(xTile.Map map, xTile.Layers.Layer back)
    {
        List<Vector2> oldSlots = TryReadLayout(map, out _, out List<Vector2> layoutSlots)
            ? layoutSlots
            : new List<Vector2>();

        foreach (Vector2 slot in oldSlots)
        {
            int x = (int)slot.X;
            int y = (int)slot.Y;
            if (x < 0 || y < 0 || x >= back.LayerWidth || y >= back.LayerHeight
                || back.Tiles[x, y] is not { } tile
                || !tile.Properties.ContainsKey(FeedingSlotProperty))
            {
                continue;
            }

            tile.Properties.Remove(FeedingSlotProperty);
            tile.Properties.Remove("Trough");
        }

        if (oldSlots.Count == 0)
        {
            for (int x = 0; x < back.LayerWidth; x++)
            for (int y = 0; y < back.LayerHeight; y++)
            {
                if (back.Tiles[x, y] is not { } tile || !tile.Properties.ContainsKey(FeedingSlotProperty))
                    continue;

                tile.Properties.Remove(FeedingSlotProperty);
                tile.Properties.Remove("Trough");
            }
        }

        map.Properties.Remove(FeedingLayoutProperty);
    }

    private static bool TryReadLayout(xTile.Map map, out int targetCount, out List<Vector2> slots)
    {
        targetCount = 0;
        slots = new List<Vector2>();
        if (!map.Properties.ContainsKey(FeedingLayoutProperty))
            return false;

        string[] fields = map.Properties[FeedingLayoutProperty].ToString().Split('|', 2);
        if (fields.Length != 2
            || !int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out targetCount))
        {
            return false;
        }

        if (fields[1].Length == 0)
            return true;

        foreach (string encodedSlot in fields[1].Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] coordinates = encodedSlot.Split(',');
            if (coordinates.Length != 2
                || !int.TryParse(coordinates[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)
                || !int.TryParse(coordinates[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
            {
                slots.Clear();
                return false;
            }

            slots.Add(new Vector2(x, y));
        }

        return true;
    }

    private static string SerializeLayout(int targetCount, IEnumerable<Vector2> slots)
    {
        string encodedSlots = string.Join(";", slots.Select(slot =>
            $"{((int)slot.X).ToString(CultureInfo.InvariantCulture)},{((int)slot.Y).ToString(CultureInfo.InvariantCulture)}"));
        return $"{targetCount.ToString(CultureInfo.InvariantCulture)}|{encodedSlots}";
    }
}
