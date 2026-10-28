using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;

namespace taums_slime_ranching.Services;

/// <summary>Uses a vanilla 3x2 furniture footprint for the refinery and mutation pool.</summary>
internal static class OtherMachinePlacementGeometry
{
    private const int TileSize = 64;
    private const int Width = 3;
    private const int Height = 2;
    private const int AnchorOffsetX = -1;
    private static Furniture? footprintPreview;
    private static StardewValley.Object? currentMachine;

    public static bool IsSupported(StardewValley.Object obj) => obj.QualifiedItemId == GelRefineryContent.QualifiedItemId
        || obj.QualifiedItemId == MutagenicPoolContent.QualifiedItemId;

    public static bool IsFootprintPreview(Furniture furniture) => ReferenceEquals(furniture, footprintPreview);

    public static bool DrawPlacement(StardewValley.Object machine, SpriteBatch batch, GameLocation location)
    {
        currentMachine = machine;
        footprintPreview ??= ItemRegistry.Create<Furniture>("(F)NauticalRug");
        footprintPreview.TileLocation = machine.TileLocation + new Vector2(AnchorOffsetX, 0);
        footprintPreview.drawPlacementBounds(batch, location);
        return false;
    }

    public static void DrawGhost(SpriteBatch batch, int x, int y, float alpha)
    {
        if (currentMachine is null)
            return;

        string textureAsset = currentMachine.QualifiedItemId switch
        {
            var id when id == GelRefineryContent.QualifiedItemId => "Mods/Taum.SlimeRanching/Machines/GelRefinery",
            _ => "Mods/Taum.SlimeRanching/Machines/MutagenicPool"
        };
        Texture2D texture = Game1.content.Load<Texture2D>(textureAsset);
        Vector2 anchor = new((int)Math.Floor(x / (double)TileSize), (int)Math.Floor(y / (double)TileSize));
        Vector2 world = new(anchor.X * TileSize + TileSize, (anchor.Y + Height) * TileSize - 2 * TileSize);
        Vector2 screen = Game1.GlobalToLocal(Game1.viewport, world);
        Rectangle destination = new((int)screen.X, (int)screen.Y, TileSize, 2 * TileSize);
        float depth = Math.Clamp(((anchor.Y + Height) * TileSize - 8) / 10000f, 0f, 0.999f);
        Rectangle source = new(0, 0, texture.Width, texture.Height);
        batch.Draw(texture, destination, source, Color.White * alpha,
            0f, Vector2.Zero, SpriteEffects.None, depth);
    }

    public static bool IsClear(GameLocation location, Vector2 anchor)
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

    public static bool TryPlace(StardewValley.Object machine, GameLocation location, ref int x, ref int y, ref bool result)
    {
        if (!IsSupported(machine))
            return true;

        Vector2 anchor = new((int)Math.Floor(x / (double)TileSize) + AnchorOffsetX,
            (int)Math.Floor(y / (double)TileSize));
        if (!IsClear(location, anchor))
        {
            result = false;
            return false;
        }

        x += AnchorOffsetX * TileSize;
        return true;
    }

    public static void RedirectToolHit(GameLocation location, ref int x, ref int y)
    {
        int tileX = x / TileSize;
        int tileY = y / TileSize;
        for (int anchorY = tileY - Height + 1; anchorY <= tileY; anchorY++)
        for (int anchorX = tileX - Width + 1; anchorX <= tileX; anchorX++)
        {
            Vector2 anchor = new(anchorX, anchorY);
            if (tileX < anchorX || tileX >= anchorX + Width || tileY < anchorY || tileY >= anchorY + Height
                || !location.Objects.TryGetValue(anchor, out StardewValley.Object? obj) || !IsSupported(obj))
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
        OtherMachinePlacementGeometry.TryPlace(__instance, location, ref x, ref y, ref __result);
}

[HarmonyPatch(typeof(Furniture), nameof(Furniture.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) })]
internal static class OtherMachineFurniturePreviewPatch
{
    private static bool Prefix(Furniture __instance, SpriteBatch spriteBatch, int x, int y, float alpha)
    {
        if (!OtherMachinePlacementGeometry.IsFootprintPreview(__instance))
            return true;

        OtherMachinePlacementGeometry.DrawGhost(spriteBatch, x, y, alpha);
        return false;
    }
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
