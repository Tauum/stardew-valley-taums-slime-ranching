using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;

namespace taums_slime_ranching.Services;

/// <summary>Shows and enforces the breeder's full 3x2 placement footprint.</summary>
internal static class BreederFootprintGeometry
{
    // Vanilla Nautical Rug is placeable furniture with a native 3x2 footprint.
    private const string VanillaThreeByTwoFurnitureId = "(F)NauticalRug";
    private static Furniture? placementPreview;
    public const int TileSize = 64;
    public const int Width = 3;
    public const int Height = 2;
    public const int FootprintOffsetY = 0;
    // Treat Stardew's cursor tile as the center of this wide machine, so a tile
    // directly in front of the farmer doesn't anchor all five tiles to the right.
    public const int AnchorOffsetX = -(Width / 2);

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

    public static void Draw(SpriteBatch batch, GameLocation location, Vector2 anchor)
    {
        placementPreview ??= ItemRegistry.Create<Furniture>(VanillaThreeByTwoFurnitureId);
        placementPreview.TileLocation = anchor;
        placementPreview.drawPlacementBounds(batch, location);
    }

    public static bool IsPlacementPreview(Furniture furniture) => ReferenceEquals(furniture, placementPreview);
}

[HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.drawPlacementBounds), new[] { typeof(SpriteBatch), typeof(GameLocation) })]
internal static class BreederPlacementBoundsPatch
{
    private static bool Prefix(StardewValley.Object __instance, SpriteBatch spriteBatch, GameLocation location)
    {
        if (__instance.QualifiedItemId != ConverterContent.BreederQualifiedId)
            return true;

        Vector2 anchor = __instance.TileLocation + new Vector2(BreederFootprintGeometry.AnchorOffsetX, 0);
        BreederFootprintGeometry.Draw(spriteBatch, location, anchor);
        return false;
    }
}

[HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.placementAction), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) })]
internal static class BreederPlacementActionPatch
{
    private static bool Prefix(StardewValley.Object __instance, GameLocation location, ref int x, ref int y, Farmer who, ref bool __result)
    {
        if (__instance.QualifiedItemId != ConverterContent.BreederQualifiedId)
            return true;

        int cursorTileX = (int)Math.Floor(x / (double)BreederFootprintGeometry.TileSize);
        int cursorTileY = (int)Math.Floor(y / (double)BreederFootprintGeometry.TileSize);
        Vector2 anchor = new(cursorTileX + BreederFootprintGeometry.AnchorOffsetX,
            cursorTileY + BreederFootprintGeometry.FootprintOffsetY);
        if (BreederFootprintGeometry.IsAreaClear(location, anchor))
        {
            // Store the object at the left edge of the centered footprint. Vanilla
            // placementAction uses these pixels to choose the object's dictionary key.
            x += BreederFootprintGeometry.AnchorOffsetX * BreederFootprintGeometry.TileSize;
            y += BreederFootprintGeometry.FootprintOffsetY * BreederFootprintGeometry.TileSize;
            return true;
        }

        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(Furniture), nameof(Furniture.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) })]
internal static class BreederFurniturePreviewDrawPatch
{
    private static bool Prefix(Furniture __instance, SpriteBatch spriteBatch, int x, int y, float alpha)
    {
        if (!BreederFootprintGeometry.IsPlacementPreview(__instance))
            return true;

        // Keep the ghost tied to Stardew's live placement-preview coordinates as the
        // mouse moves; the furniture tile location can lag and leave a second ghost.
        BreederDrawPatch.DrawPlacementGhost(spriteBatch, x, y, alpha);
        return false;
    }
}

/// <summary>Routes axe and pickaxe strikes anywhere in the breeder footprint to its anchor tile.</summary>
internal static class BreederToolRemovalGeometry
{
    public static void RedirectHit(GameLocation location, ref int x, ref int y)
    {
        int tileX = x / BreederFootprintGeometry.TileSize;
        int tileY = y / BreederFootprintGeometry.TileSize;

        int firstAnchorX = tileX - (BreederFootprintGeometry.Width - 1);
        int lastAnchorX = tileX;
        int firstAnchorY = tileY - BreederFootprintGeometry.FootprintOffsetY - (BreederFootprintGeometry.Height - 1);
        int lastAnchorY = tileY - BreederFootprintGeometry.FootprintOffsetY;

        for (int anchorY = firstAnchorY; anchorY <= lastAnchorY; anchorY++)
        for (int anchorX = firstAnchorX; anchorX <= lastAnchorX; anchorX++)
        {
            Vector2 anchor = new(anchorX, anchorY);
            if (tileX < anchorX || tileX >= anchorX + BreederFootprintGeometry.Width
                || tileY < anchorY + BreederFootprintGeometry.FootprintOffsetY
                || tileY >= anchorY + BreederFootprintGeometry.FootprintOffsetY + BreederFootprintGeometry.Height)
                continue;

            if (!location.Objects.TryGetValue(anchor, out StardewValley.Object? obj)
                || obj.QualifiedItemId != ConverterContent.BreederQualifiedId)
                continue;

            // Axe and pickaxe resolve the object using the tile derived from x/y.
            // Point them at the actual dictionary key so vanilla tool behavior removes it.
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
