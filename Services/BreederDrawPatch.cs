using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace taums_slime_ranching.Services;

/// <summary>Draws the large breeder in the world-sort pass instead of over the finished scene.</summary>
[HarmonyPatch]
internal static class BreederDrawPatch
{
    private const int TileSize = 64;
    private const int FootprintWidth = 5;
    private const int FootprintHeight = 2;
    private const int SpriteHeight = 4 * TileSize;
    private const int PlacedSpriteOffsetY = 0;
    // Crop the outer transparent glow/pipe margins so the solid breeder frame spans
    // the five-tile footprint when rendered at the footprint width.
    private const int SourceX = 275;
    private const int SourceWidth = 1472;
    private static Texture2D? texture;

    public static void DrawPlacementGhost(SpriteBatch batch, int tileX, int tileY, float alpha)
    {
        Texture2D breederTexture = GetTexture();
        int width = FootprintWidth * TileSize;
        int height = SpriteHeight;
        int bottom = (tileY + FootprintHeight) * TileSize;
        Vector2 worldTopLeft = new(tileX * TileSize, bottom - height);
        Vector2 screenTopLeft = Game1.GlobalToLocal(Game1.viewport, worldTopLeft);
        Rectangle destination = new((int)screenTopLeft.X, (int)screenTopLeft.Y, width, height);
        Rectangle source = GetBreederSource(breederTexture);
        float depth = Math.Clamp((bottom - 8) / 10000f, 0f, 0.999f);
        batch.Draw(breederTexture, destination, source, Color.White * alpha, 0f, Vector2.Zero, SpriteEffects.None, depth);
    }

    private static Texture2D GetTexture() => texture ??= Game1.content.Load<Texture2D>("Mods/Taum.SlimeRanching/Machines/SlimeBreeder");

    private static IEnumerable<MethodBase> TargetMethods() => typeof(StardewValley.Object)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Where(method => method.Name == "draw"
            && method.GetParameters().FirstOrDefault()?.ParameterType == typeof(SpriteBatch)
            && method.GetParameters().Count(parameter => parameter.ParameterType == typeof(int)) >= 2);

    private static bool Prefix(StardewValley.Object __instance, object[] __args)
    {
        if (__instance.QualifiedItemId != ConverterContent.BreederQualifiedId)
            return true;

        SpriteBatch batch = __args.OfType<SpriteBatch>().First();
        Texture2D breederTexture = GetTexture();

        Vector2 tile = __instance.TileLocation;
        int width = FootprintWidth * TileSize;
        Vector2 worldTopLeft = new(
            tile.X * TileSize,
            (tile.Y + FootprintHeight) * TileSize - SpriteHeight + PlacedSpriteOffsetY);
        Vector2 screenTopLeft = Game1.GlobalToLocal(Game1.viewport, worldTopLeft);
        Rectangle destination = new((int)screenTopLeft.X, (int)screenTopLeft.Y, width, SpriteHeight);
        Rectangle source = GetBreederSource(breederTexture);

        // Sort from the bottom of the actual 5x2 collision footprint. This keeps the
        // tall sprite behind characters and objects standing in front of the machine.
        float depth = Math.Clamp(
            ((tile.Y + BreederFootprintGeometry.FootprintOffsetY + FootprintHeight) * TileSize - 8) / 10000f,
            0f,
            0.999f);
        batch.Draw(breederTexture, destination, source, Color.White, 0f, Vector2.Zero, SpriteEffects.None, depth);
        return false;
    }

    private static Rectangle GetBreederSource(Texture2D breederTexture) =>
        new(SourceX, 0, Math.Min(SourceWidth, breederTexture.Width - SourceX), breederTexture.Height);
}
