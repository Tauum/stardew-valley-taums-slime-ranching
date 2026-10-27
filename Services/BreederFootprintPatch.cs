using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System.Runtime.CompilerServices;

namespace taums_slime_ranching.Services;

/// <summary>Shows and enforces the breeder's full 5x2 placement footprint.</summary>
internal static class BreederFootprintGeometry
{
    public const int TileSize = 64;
    public const int Width = 5;
    public const int Height = 2;
    public const int FootprintOffsetY = -1;
    public static bool IsAreaClear(GameLocation location, Vector2 anchor)
    {
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            Vector2 tile = anchor + new Vector2(x, y + FootprintOffsetY);
            if (!location.isTilePlaceable(tile, true)
                || location.Objects.ContainsKey(tile)
                || location.terrainFeatures.ContainsKey(tile))
                return false;
        }

        return true;
    }

    public static void Draw(StardewValley.Object placementItem, SpriteBatch batch, GameLocation location, Vector2 anchor)
    {
        Vector2 originalItemTile = placementItem.TileLocation;
        try
        {
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                placementItem.TileLocation = anchor + new Vector2(x, y + FootprintOffsetY);
                VanillaPlacementBounds.Draw(placementItem, batch, location);
            }
        }
        finally
        {
            placementItem.TileLocation = originalItemTile;
        }
    }
}

[HarmonyReversePatch]
[HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.drawPlacementBounds), new[] { typeof(SpriteBatch), typeof(GameLocation) })]
internal static class VanillaPlacementBounds
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Draw(StardewValley.Object instance, SpriteBatch spriteBatch, GameLocation location) =>
        throw new NotImplementedException("Harmony replaces this stub with Stardew's original placement preview.");
}

[HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.drawPlacementBounds), new[] { typeof(SpriteBatch), typeof(GameLocation) })]
internal static class BreederPlacementBoundsPatch
{
    private static bool Prefix(StardewValley.Object __instance, SpriteBatch spriteBatch, GameLocation location)
    {
        if (__instance.QualifiedItemId != ConverterContent.BreederQualifiedId)
            return true;

        BreederFootprintGeometry.Draw(__instance, spriteBatch, location, __instance.TileLocation);
        return false;
    }
}

[HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.placementAction), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) })]
internal static class BreederPlacementActionPatch
{
    private static bool Prefix(StardewValley.Object __instance, GameLocation location, int x, int y, Farmer who, ref bool __result)
    {
        if (__instance.QualifiedItemId != ConverterContent.BreederQualifiedId)
            return true;

        Vector2 anchor = new((int)Math.Floor(x / (double)BreederFootprintGeometry.TileSize),
            (int)Math.Floor(y / (double)BreederFootprintGeometry.TileSize));
        if (BreederFootprintGeometry.IsAreaClear(location, anchor))
            return true;

        __result = false;
        return false;
    }
}
