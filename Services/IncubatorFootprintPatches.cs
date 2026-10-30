using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;

namespace taums_slime_ranching.Services;

/// <summary>Extends movement collision across the mod's oversized ranching machines.</summary>
[HarmonyPatch]
internal static class IncubatorFootprintPatches
{
    private const int TileSize = 64;
    private const int MaximumFootprintWidth = 5;
    private const int MaximumFootprintHeight = 2;

    private static IEnumerable<MethodBase> TargetMethods() => typeof(GameLocation)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Where(method => method.Name == "isCollidingPosition"
            && method.ReturnType == typeof(bool)
            && method.GetParameters().FirstOrDefault()?.ParameterType == typeof(Rectangle));

    private static void Postfix(GameLocation __instance, Rectangle position, object[] __args, ref bool __result)
    {
        if (__result)
            return;

        Character? character = __args.OfType<Character>().FirstOrDefault();
        Rectangle currentBounds = character?.GetBoundingBox() ?? Rectangle.Empty;

        int firstAnchorX = (int)Math.Floor(position.Left / (double)TileSize) - (MaximumFootprintWidth - 1);
        int lastAnchorX = (int)Math.Floor((position.Right - 1) / (double)TileSize);
        int firstAnchorY = (int)Math.Floor(position.Top / (double)TileSize) - (MaximumFootprintHeight - 1);
        int lastAnchorY = (int)Math.Floor((position.Bottom - 1) / (double)TileSize);
        for (int tileY = firstAnchorY; tileY <= lastAnchorY; tileY++)
        {
            for (int tileX = firstAnchorX; tileX <= lastAnchorX; tileX++)
            {
                if (!__instance.Objects.TryGetValue(new Vector2(tileX, tileY), out StardewValley.Object? obj))
                    continue;

                int footprintWidth = obj.QualifiedItemId switch
                {
                    RanchingItemContent.IncubatorQualifiedId or SlimePrinterContent.QualifiedItemId
                        or MutagenicPoolContent.QualifiedItemId => 3,
                    RanchingItemContent.BreederQualifiedId => 5,
                    _ => 0
                };
                int footprintHeight = obj.QualifiedItemId switch
                {
                    RanchingItemContent.IncubatorQualifiedId or RanchingItemContent.BreederQualifiedId => 2,
                    SlimePrinterContent.QualifiedItemId or MutagenicPoolContent.QualifiedItemId => 2,
                    _ => 0
                };
                if (footprintWidth == 0)
                    continue;

                int footprintTopY = tileY + (obj.QualifiedItemId == RanchingItemContent.BreederQualifiedId
                    ? BreederFootprintGeometry.FootprintOffsetY
                    : 0);
                Rectangle footprint = new(tileX * TileSize, footprintTopY * TileSize,
                    footprintWidth * TileSize, footprintHeight * TileSize);
                if (!position.Intersects(footprint))
                    continue;

                // Existing saves may place the player inside a newly added footprint. Let them
                // walk out, while still blocking movement that increases their overlap with it.
                if (character is not null && currentBounds.Intersects(footprint)
                    && GetOverlapArea(position, footprint) < GetOverlapArea(currentBounds, footprint))
                    continue;

                __result = true;
                return;
            }
        }
    }

    private static long GetOverlapArea(Rectangle first, Rectangle second)
    {
        int width = Math.Max(0, Math.Min(first.Right, second.Right) - Math.Max(first.Left, second.Left));
        int height = Math.Max(0, Math.Min(first.Bottom, second.Bottom) - Math.Max(first.Top, second.Top));
        return (long)width * height;
    }
}
