using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Tools;

namespace taums_slime_ranching.Services;

/// <summary>Placement and tool routing for the pool, printer, and Research Lab.</summary>
internal static class OtherMachinePlacementGeometry
{
    private const int TileSize = 64;

    public static bool IsSupported(StardewValley.Object obj) =>
        SlimePrinterContent.IsSlimePrinter(obj)
        || obj.QualifiedItemId == MutagenicPoolContent.QualifiedItemId
        || ResearchLabContent.IsResearchLab(obj);

    public static bool DrawPlacement(StardewValley.Object machine, SpriteBatch batch, GameLocation location)
    {
        MachineArtContent.DrawPlacementBounds(batch, location, machine);
        return false;
    }

    public static bool TryPlace(StardewValley.Object machine, GameLocation location, ref int x, ref int y,
        ref bool result) => MachineArtContent.TryPlace(machine, location, ref x, ref y, ref result);

    public static void RedirectToolHit(GameLocation location, ref int x, ref int y)
    {
        int tileX = x / TileSize;
        int tileY = y / TileSize;
        for (int anchorY = tileY - 2; anchorY <= tileY; anchorY++)
        for (int anchorX = tileX - 3; anchorX <= tileX; anchorX++)
        {
            Vector2 anchor = new(anchorX, anchorY);
            if (!location.Objects.TryGetValue(anchor, out StardewValley.Object? obj)
                || !IsSupported(obj)
                || !MachineArtContent.IsWithinFootprint(obj, anchor, new Vector2(tileX, tileY)))
                continue;

            x = anchorX * TileSize + TileSize / 2;
            y = anchorY * TileSize + TileSize / 2;
            return;
        }
    }
}

[HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.drawPlacementBounds), new[] { typeof(SpriteBatch), typeof(GameLocation) })]
internal static class OtherMachinePlacementBoundsPatch
{
    private static bool Prefix(StardewValley.Object __instance, SpriteBatch spriteBatch, GameLocation location)
    {
        if (!OtherMachinePlacementGeometry.IsSupported(__instance))
            return true;

        return OtherMachinePlacementGeometry.DrawPlacement(__instance, spriteBatch, location);
    }
}

[HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.placementAction), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) })]
internal static class OtherMachinePlacementActionPatch
{
    private static bool Prefix(StardewValley.Object __instance, GameLocation location, ref int x, ref int y, ref bool __result) =>
        !OtherMachinePlacementGeometry.IsSupported(__instance)
            || OtherMachinePlacementGeometry.TryPlace(__instance, location, ref x, ref y, ref __result);
}

[HarmonyPatch(typeof(Axe), nameof(Axe.DoFunction), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(int), typeof(Farmer) })]
internal static class OtherMachineAxeRemovalPatch
{
    private static void Prefix(GameLocation location, ref int x, ref int y) => OtherMachinePlacementGeometry.RedirectToolHit(location, ref x, ref y);
}

[HarmonyPatch(typeof(Pickaxe), nameof(Pickaxe.DoFunction), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(int), typeof(Farmer) })]
internal static class OtherMachinePickaxeRemovalPatch
{
    private static void Prefix(GameLocation location, ref int x, ref int y) => OtherMachinePlacementGeometry.RedirectToolHit(location, ref x, ref y);
}
