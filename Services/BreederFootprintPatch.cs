using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Tools;
using Object = StardewValley.Object;

namespace taums_slime_ranching.Services;

/// <summary>Shows and enforces the approved four-by-two breeder footprint.</summary>
internal static class BreederFootprintGeometry
{
    public const int TileSize = 64;
    public const int Width = 4;
    public const int Height = 2;
}

[HarmonyPatch(typeof(Object), nameof(Object.drawPlacementBounds), new[] { typeof(SpriteBatch), typeof(GameLocation) })]
internal static class BreederPlacementBoundsPatch
{
    private static bool Prefix(Object __instance, SpriteBatch spriteBatch, GameLocation location)
    {
        if (__instance.QualifiedItemId != RanchingItemContent.BreederQualifiedId)
            return true;

        MachineArtContent.DrawPlacementBounds(spriteBatch, location, __instance);
        return false;
    }
}

[HarmonyPatch(typeof(Object), nameof(Object.placementAction), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) })]
internal static class BreederPlacementActionPatch
{
    private static bool Prefix(Object __instance, GameLocation location, ref int x, ref int y, ref bool __result)
    {
        if (__instance.QualifiedItemId != RanchingItemContent.BreederQualifiedId)
            return true;

        return MachineArtContent.TryPlace(__instance, location, ref x, ref y, ref __result);
    }
}

internal static class BreederToolRemovalGeometry
{
    public static void RedirectHit(GameLocation location, ref int x, ref int y)
    {
        int tileX = x / BreederFootprintGeometry.TileSize;
        int tileY = y / BreederFootprintGeometry.TileSize;
        for (int anchorY = tileY - BreederFootprintGeometry.Height + 1; anchorY <= tileY; anchorY++)
        for (int anchorX = tileX - BreederFootprintGeometry.Width + 1; anchorX <= tileX; anchorX++)
        {
            Vector2 anchor = new(anchorX, anchorY);
            if (tileX < anchorX || tileX >= anchorX + BreederFootprintGeometry.Width
                || tileY < anchorY || tileY >= anchorY + BreederFootprintGeometry.Height
                || !location.Objects.TryGetValue(anchor, out Object? obj)
                || obj.QualifiedItemId != RanchingItemContent.BreederQualifiedId)
                continue;

            x = anchorX * BreederFootprintGeometry.TileSize + BreederFootprintGeometry.TileSize / 2;
            y = anchorY * BreederFootprintGeometry.TileSize + BreederFootprintGeometry.TileSize / 2;
            return;
        }
    }
}

[HarmonyPatch(typeof(Axe), nameof(Axe.DoFunction), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(int), typeof(Farmer) })]
internal static class BreederAxeRemovalPatch
{
    private static void Prefix(GameLocation location, ref int x, ref int y) =>
        BreederToolRemovalGeometry.RedirectHit(location, ref x, ref y);
}

[HarmonyPatch(typeof(Pickaxe), nameof(Pickaxe.DoFunction), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(int), typeof(Farmer) })]
internal static class BreederPickaxeRemovalPatch
{
    private static void Prefix(GameLocation location, ref int x, ref int y) =>
        BreederToolRemovalGeometry.RedirectHit(location, ref x, ref y);
}
