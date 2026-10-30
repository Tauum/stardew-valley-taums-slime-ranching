using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Tools;

namespace taums_slime_ranching.Services;

/// <summary>Shares the approved two-by-three incubator dimensions with its menus and tool routing.</summary>
internal static class IncubatorPlacementGeometry
{
    public const int TileSize = 64;
    public const int Width = 2;
    public const int Height = 3;

    public static Texture2D Texture => MachineArtContent.GetTexture(MachineArtContent.IncubatorTextureName);

    public static bool IsIncubator(StardewValley.Object obj) =>
        obj.QualifiedItemId == RanchingItemContent.IncubatorQualifiedId;
}

[HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.drawPlacementBounds), new[] { typeof(SpriteBatch), typeof(GameLocation) })]
internal static class IncubatorPlacementBoundsPatch
{
    private static bool Prefix(StardewValley.Object __instance, SpriteBatch spriteBatch, GameLocation location)
    {
        if (!IncubatorPlacementGeometry.IsIncubator(__instance))
            return true;

        MachineArtContent.DrawPlacementBounds(spriteBatch, location, __instance);
        return false;
    }
}

[HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.placementAction), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) })]
internal static class IncubatorPlacementActionPatch
{
    private static bool Prefix(StardewValley.Object __instance, GameLocation location, ref int x, ref int y, ref bool __result)
    {
        if (!IncubatorPlacementGeometry.IsIncubator(__instance))
            return true;

        return MachineArtContent.TryPlace(__instance, location, ref x, ref y, ref __result);
    }
}

[HarmonyPatch]
internal static class IncubatorDrawPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(StardewValley.Object)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Where(method => method.Name == "draw"
            && method.GetParameters().FirstOrDefault()?.ParameterType == typeof(SpriteBatch)
            && method.GetParameters().Count(parameter => parameter.ParameterType == typeof(int)) >= 2);

    private static bool Prefix(StardewValley.Object __instance, object[] __args)
    {
        if (!IncubatorPlacementGeometry.IsIncubator(__instance))
            return true;

        SpriteBatch batch = __args.OfType<SpriteBatch>().First();
        float alpha = __args.OfType<float>().DefaultIfEmpty(1f).First();
        MachineArtContent.DrawWorld(batch, __instance, __instance.TileLocation, alpha);
        return false;
    }
}

internal static class IncubatorToolRemovalGeometry
{
    public static void RedirectHit(GameLocation location, ref int x, ref int y)
    {
        int tileX = x / IncubatorPlacementGeometry.TileSize;
        int tileY = y / IncubatorPlacementGeometry.TileSize;
        for (int anchorY = tileY - IncubatorPlacementGeometry.Height + 1; anchorY <= tileY; anchorY++)
        for (int anchorX = tileX - IncubatorPlacementGeometry.Width + 1; anchorX <= tileX; anchorX++)
        {
            Vector2 anchor = new(anchorX, anchorY);
            if (tileX < anchorX || tileX >= anchorX + IncubatorPlacementGeometry.Width
                || tileY < anchorY || tileY >= anchorY + IncubatorPlacementGeometry.Height
                || !location.Objects.TryGetValue(anchor, out StardewValley.Object? obj)
                || !IncubatorPlacementGeometry.IsIncubator(obj))
                continue;

            x = anchorX * IncubatorPlacementGeometry.TileSize + IncubatorPlacementGeometry.TileSize / 2;
            y = anchorY * IncubatorPlacementGeometry.TileSize + IncubatorPlacementGeometry.TileSize / 2;
            return;
        }
    }
}

[HarmonyPatch(typeof(Axe), nameof(Axe.DoFunction), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(int), typeof(Farmer) })]
internal static class IncubatorAxeRemovalPatch
{
    private static void Prefix(GameLocation location, ref int x, ref int y) => IncubatorToolRemovalGeometry.RedirectHit(location, ref x, ref y);
}

[HarmonyPatch(typeof(Pickaxe), nameof(Pickaxe.DoFunction), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(int), typeof(Farmer) })]
internal static class IncubatorPickaxeRemovalPatch
{
    private static void Prefix(GameLocation location, ref int x, ref int y) => IncubatorToolRemovalGeometry.RedirectHit(location, ref x, ref y);
}
