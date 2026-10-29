using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace taums_slime_ranching.Services;

/// <summary>
/// Reuses Stardew's decoration engine without changing the saved AnimalHouse type.
/// The adapter is never registered as a location, serialized, or used to house animals.
/// </summary>
internal static class DaycareDecorating
{
    private const string FloorPrefix = "Taum.SlimeRanching/Decor/Floor/";
    private const string WallPrefix = "Taum.SlimeRanching/Decor/Wall/";
    private static readonly Dictionary<GameLocation, Room> Rooms = new();
    private static readonly Dictionary<GameLocation, Map> FailedMaps = new();
    private static IMonitor? monitor;

    internal sealed record Room(AnimalHouse House, Map Map, DecoratableLocation Decorator);

    public static void Initialize(IModHelper helper, IMonitor log)
    {
        monitor = log;
        helper.Events.Player.Warped += (sender, e) =>
        {
            if (e.IsLocalPlayer)
                TryGetRoom(e.NewLocation, out _);
        };
        helper.Events.GameLoop.SaveLoaded += (sender, e) =>
        {
            Clear();
            TryGetRoom(Game1.currentLocation, out _);
        };
        helper.Events.GameLoop.DayStarted += (sender, e) => TryGetRoom(Game1.currentLocation, out _);
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => Clear();
    }

    private static void Clear()
    {
        Rooms.Clear();
        FailedMaps.Clear();
    }

    internal static bool TryGetRoom(GameLocation location, out Room room)
    {
        room = null!;
        if (!Context.IsWorldReady || location is not AnimalHouse house ||
            !BuildingContent.IsSlimeDaycare(house.ParentBuilding?.buildingType.Value))
            return false;

        Map map = house.Map;
        if (Rooms.TryGetValue(house, out Room? cached) && ReferenceEquals(cached.Map, map))
        {
            room = cached;
            return true;
        }
        if (FailedMaps.TryGetValue(house, out Map? failed) && ReferenceEquals(failed, map))
            return false;

        try
        {
            // Native SetFloor/SetWallpaper use this stock sheet and support the
            // additional sets from Data/AdditionalWallpaperFlooring themselves.
            if (map.GetTileSheet("walls_and_floors") is null)
            {
                Texture2D texture = Game1.content.Load<Texture2D>("Maps/walls_and_floors");
                map.AddTileSheet(new TileSheet("walls_and_floors", map, "Maps/walls_and_floors",
                    new Size(texture.Width / 16, texture.Height / 16), new Size(16, 16)));
                map.LoadTileSheets(Game1.mapDisplayDevice);
            }

            DecoratableLocation decorator = new() { map = map };
            decorator.name.Value = "Temp_DaycareDecorating";

            // Let the native reader discover FloorIDs/WallIDs and the three-row
            // wallpaper layout. Restore our default art after its automatic #0
            // initialization; only player-selected styles replace that artwork.
            List<(Layer Layer, int X, int Y, Tile? Tile)> originalTiles = new();
            foreach (string name in new[] { "Back", "Buildings" })
            {
                Layer layer = map.GetLayer(name);
                for (int y = 0; y < layer.LayerHeight; y++)
                for (int x = 0; x < layer.LayerWidth; x++)
                {
                    Tile? tile = layer.Tiles[x, y];
                    // setMapTile copies instance properties, not index properties.
                    // Keep area tags even after a different tilesheet is applied.
                    if (tile is not null)
                        foreach (string key in new[] { "FloorID", "WallID" })
                            if (tile.TileIndexProperties.TryGetValue(key, out var value))
                                tile.Properties[key] = value;
                    originalTiles.Add((layer, x, y, tile));
                }
            }
            try
            {
                decorator.ReadWallpaperAndFloorTileData();
            }
            finally
            {
                foreach (var original in originalTiles)
                    original.Layer.Tiles[original.X, original.Y] = original.Tile;
            }
            decorator.appliedFloor.Clear();
            decorator.appliedWallpaper.Clear();

            foreach (string id in decorator.floorIDs)
                if (house.modData.TryGetValue(FloorPrefix + id, out string pattern))
                    decorator.SetFloor(pattern, id);
            foreach (string id in decorator.wallpaperIDs)
                if (house.modData.TryGetValue(WallPrefix + id, out string pattern))
                    decorator.SetWallpaper(pattern, id);

            room = new Room(house, map, decorator);
            Rooms[house] = room;
            FailedMaps.Remove(house);
            return true;
        }
        catch (Exception ex)
        {
            // Don't let a bad decorating map repeatedly fail in placement preview.
            FailedMaps[house] = map;
            monitor?.Log($"Couldn't initialize Daycare wallpaper/flooring in {house.NameOrUniqueName}: {ex}", LogLevel.Error);
            return false;
        }
    }

    internal static void Persist(Room room)
    {
        foreach (var pair in room.Decorator.appliedFloor.Pairs)
            room.House.modData[FloorPrefix + pair.Key] = pair.Value;
        foreach (var pair in room.Decorator.appliedWallpaper.Pairs)
            room.House.modData[WallPrefix + pair.Key] = pair.Value;
    }
}

// Vanilla gates wallpaper placement/range on the concrete DecoratableLocation
// type. SMAPI map properties alone cannot make AnimalHouse pass that check.
// These two narrow adapters leave native placement, consumption and patterns in charge.
[HarmonyPatch(typeof(Utility), nameof(Utility.playerCanPlaceItemHere))]
internal static class DaycareWallpaperPlacementCheckPatch
{
    private static bool Prefix(ref GameLocation location, Item item, int x, int y, ref bool __result)
    {
        if (item is not Wallpaper paper || !DaycareDecorating.TryGetRoom(location, out var room))
            return true;

        int tileX = x / 64;
        int tileY = y / 64;
        string? area = paper.isFloor.Value
            ? room.Decorator.GetFloorID(tileX, tileY)
            : room.Decorator.GetWallpaperID(tileX, tileY);
        if (area is null || !paper.canBePlacedHere(location, new Vector2(tileX, tileY)))
        {
            __result = false;
            return false;
        }

        location = room.Decorator;
        return true;
    }
}

[HarmonyPatch(typeof(Wallpaper), nameof(Wallpaper.placementAction))]
internal static class DaycareWallpaperPlacementPatch
{
    private static void Prefix(ref GameLocation location, out DaycareDecorating.Room? __state)
    {
        __state = null;
        if (DaycareDecorating.TryGetRoom(location, out var room))
        {
            __state = room;
            location = room.Decorator;
        }
    }

    private static void Postfix(bool __result, DaycareDecorating.Room? __state)
    {
        if (!__result || __state is null)
            return;
        DaycareDecorating.Persist(__state);
        __state.House.playSound("coin");
    }
}
