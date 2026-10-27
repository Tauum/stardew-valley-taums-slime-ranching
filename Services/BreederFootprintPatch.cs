using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace taums_slime_ranching.Services;

/// <summary>Shows and enforces the breeder's full 5x2 placement footprint.</summary>
internal static class BreederFootprintGeometry
{
    public const int TileSize = 64;
    public const int Width = 5;
    public const int Height = 2;
    public const int FootprintOffsetY = -1;
    private const int Border = 3;

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
        Vector2 world = new(anchor.X * TileSize, (anchor.Y + FootprintOffsetY) * TileSize);
        Vector2 screen = Game1.GlobalToLocal(Game1.viewport, world);
        bool canPlace = IsAreaClear(location, anchor);
        Color fill = canPlace ? new Color(75, 225, 105, 70) : new Color(240, 65, 65, 85);
        Color edge = canPlace ? new Color(115, 255, 140, 230) : new Color(255, 100, 100, 240);
        Rectangle area = new((int)screen.X, (int)screen.Y, Width * TileSize, Height * TileSize);
        batch.Draw(Game1.staminaRect, area, fill);

        for (int column = 0; column <= Width; column++)
        {
            int x = area.X + column * TileSize;
            batch.Draw(Game1.staminaRect, new Rectangle(x - Border / 2, area.Y, Border, area.Height), edge);
        }
        for (int row = 0; row <= Height; row++)
        {
            int y = area.Y + row * TileSize;
            batch.Draw(Game1.staminaRect, new Rectangle(area.X, y - Border / 2, area.Width, Border), edge);
        }
    }
}

[HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.drawPlacementBounds), new[] { typeof(SpriteBatch), typeof(GameLocation) })]
internal static class BreederPlacementBoundsPatch
{
    private static bool Prefix(StardewValley.Object __instance, SpriteBatch spriteBatch, GameLocation location)
    {
        if (__instance.QualifiedItemId != ConverterContent.BreederQualifiedId)
            return true;

        BreederFootprintGeometry.Draw(spriteBatch, location, __instance.TileLocation);
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
