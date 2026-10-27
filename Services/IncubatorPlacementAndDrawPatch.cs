using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;

namespace taums_slime_ranching.Services;

/// <summary>Gives the incubator a four-by-two placement area and depth-sorted custom art.</summary>
internal static class IncubatorPlacementGeometry
{
    public const int TileSize = 64;
    public const int Width = 4;
    public const int Height = 2;
    private const int SpriteWidth = 3 * TileSize;
    private const int SpriteHeight = 5 * TileSize;
    private const int AnchorOffsetX = -(Width / 2);
    private static Texture2D? texture;
    private static Furniture? placementPreview;

    public static Texture2D Texture => texture ??= Game1.content.Load<Texture2D>("Mods/Taum.SlimeRanching/Machines/SlimeIncubator");

    public static Vector2 GetCenteredAnchor(int cursorX, int cursorY) =>
        new((int)Math.Floor(cursorX / (double)TileSize) + AnchorOffsetX,
            (int)Math.Floor(cursorY / (double)TileSize));

    public static bool IsAreaClear(GameLocation location, Vector2 anchor)
    {
        for (int dy = 0; dy < Height; dy++)
        for (int dx = 0; dx < Width; dx++)
        {
            Vector2 tile = anchor + new Vector2(dx, dy);
            if (!location.isTilePlaceable(tile, true) || location.Objects.ContainsKey(tile)
                || location.terrainFeatures.ContainsKey(tile))
                return false;
        }
        return true;
    }

    public static void DrawPlacement(SpriteBatch batch, GameLocation location, Vector2 anchor)
    {
        placementPreview ??= ItemRegistry.Create<Furniture>("(F)814");
        placementPreview.TileLocation = anchor;
        placementPreview.drawPlacementBounds(batch, location);

        Color fill = IsAreaClear(location, anchor) ? new Color(72, 190, 92, 80) : new Color(220, 70, 70, 90);
        Color edge = IsAreaClear(location, anchor) ? new Color(72, 220, 96, 190) : new Color(240, 80, 80, 210);
        for (int dy = 0; dy < Height; dy++)
        for (int dx = 0; dx < Width; dx++)
        {
            Vector2 world = (anchor + new Vector2(dx, dy)) * TileSize;
            Vector2 screen = Game1.GlobalToLocal(Game1.viewport, world);
            Rectangle tile = new((int)screen.X, (int)screen.Y, TileSize, TileSize);
            batch.Draw(Game1.staminaRect, tile, fill);
            batch.Draw(Game1.staminaRect, new Rectangle(tile.X, tile.Y, TileSize, 2), edge);
            batch.Draw(Game1.staminaRect, new Rectangle(tile.X, tile.Bottom - 2, TileSize, 2), edge);
            batch.Draw(Game1.staminaRect, new Rectangle(tile.X, tile.Y, 2, TileSize), edge);
            batch.Draw(Game1.staminaRect, new Rectangle(tile.Right - 2, tile.Y, 2, TileSize), edge);
        }
    }

    public static void DrawWorld(SpriteBatch batch, Vector2 anchor, float alpha)
    {
        // The machine artwork is one tile narrower than its 4x2 placement/collision footprint;
        // keep its left edge anchored and trim the visual bounds on the right.
        Vector2 worldTopLeft = new(anchor.X * TileSize, (anchor.Y + Height) * TileSize - SpriteHeight);
        Vector2 screenTopLeft = Game1.GlobalToLocal(Game1.viewport, worldTopLeft);
        Rectangle destination = new((int)screenTopLeft.X, (int)screenTopLeft.Y, SpriteWidth, SpriteHeight);
        float depth = Math.Clamp(((anchor.Y + Height) * TileSize - 8) / 10000f, 0f, 0.999f);
        batch.Draw(Texture, destination, new Rectangle(0, 0, Texture.Width, Texture.Height), Color.White * alpha,
            0f, Vector2.Zero, SpriteEffects.None, depth);
    }

    public static void DrawGhost(SpriteBatch batch, int x, int y, float alpha)
    {
        Vector2 anchor = new((int)Math.Floor(x / (double)TileSize), (int)Math.Floor(y / (double)TileSize));
        Vector2 worldTopLeft = new(anchor.X * TileSize, (anchor.Y + Height) * TileSize - SpriteHeight);
        Vector2 screenTopLeft = Game1.GlobalToLocal(Game1.viewport, worldTopLeft);
        Rectangle destination = new((int)screenTopLeft.X, (int)screenTopLeft.Y, SpriteWidth, SpriteHeight);
        float depth = Math.Clamp(((anchor.Y + Height) * TileSize - 8) / 10000f, 0f, 0.999f);
        batch.Draw(Texture, destination, new Rectangle(0, 0, Texture.Width, Texture.Height), Color.White * alpha,
            0f, Vector2.Zero, SpriteEffects.None, depth);
    }

    public static bool IsIncubator(StardewValley.Object obj) => obj.QualifiedItemId == ConverterContent.IncubatorQualifiedId;
    public static bool IsPlacementPreview(Furniture furniture) => ReferenceEquals(furniture, placementPreview);
}

[HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.drawPlacementBounds), new[] { typeof(SpriteBatch), typeof(GameLocation) })]
internal static class IncubatorPlacementBoundsPatch
{
    private static bool Prefix(StardewValley.Object __instance, SpriteBatch spriteBatch, GameLocation location)
    {
        if (!IncubatorPlacementGeometry.IsIncubator(__instance))
            return true;

        Vector2 anchor = __instance.TileLocation + new Vector2(-(IncubatorPlacementGeometry.Width / 2), 0);
        IncubatorPlacementGeometry.DrawPlacement(spriteBatch, location, anchor);
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

        Vector2 anchor = IncubatorPlacementGeometry.GetCenteredAnchor(x, y);
        if (!IncubatorPlacementGeometry.IsAreaClear(location, anchor))
        {
            __result = false;
            return false;
        }

        x += -(IncubatorPlacementGeometry.Width / 2) * IncubatorPlacementGeometry.TileSize;
        return true;
    }
}

[HarmonyPatch]
internal static class IncubatorDrawPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(StardewValley.Object)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Where(method => method.Name == "draw" && method.GetParameters().FirstOrDefault()?.ParameterType == typeof(SpriteBatch)
            && method.GetParameters().Count(parameter => parameter.ParameterType == typeof(int)) >= 2
            && !(method.GetParameters() is { Length: 4 } parameters
                && parameters[1].ParameterType == typeof(int)
                && parameters[2].ParameterType == typeof(int)
                && parameters[3].ParameterType == typeof(float)));

    private static bool Prefix(StardewValley.Object __instance, object[] __args)
    {
        if (!IncubatorPlacementGeometry.IsIncubator(__instance))
            return true;

        SpriteBatch batch = __args.OfType<SpriteBatch>().First();
        Vector2 anchor = __instance.TileLocation;
        IncubatorPlacementGeometry.DrawWorld(batch, anchor, 1f);
        return false;
    }
}

[HarmonyPatch(typeof(Furniture), nameof(Furniture.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) })]
internal static class IncubatorFurniturePlacementGhostPatch
{
    private static bool Prefix(Furniture __instance, SpriteBatch spriteBatch, int x, int y, float alpha)
    {
        if (!IncubatorPlacementGeometry.IsPlacementPreview(__instance))
            return true;

        IncubatorPlacementGeometry.DrawGhost(spriteBatch, x, y, alpha);
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
