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
    private const int SourceWidth = 1949;
    private static Texture2D? texture;

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
        texture ??= Game1.content.Load<Texture2D>("Mods/Taum.SlimeRanching/Machines/SlimeBreeder");

        Vector2 tile = __instance.TileLocation;
        int width = FootprintWidth * TileSize;
        Vector2 worldTopLeft = new(tile.X * TileSize, (tile.Y + FootprintHeight) * TileSize - SpriteHeight);
        Vector2 screenTopLeft = Game1.GlobalToLocal(Game1.viewport, worldTopLeft);
        Rectangle destination = new((int)screenTopLeft.X, (int)screenTopLeft.Y, width, SpriteHeight);
        Rectangle source = new(0, 0, Math.Min(SourceWidth, texture.Width), texture.Height);

        // World sprites use their ground contact point for depth, allowing a character
        // walking in front of the machine to render over it.
        float depth = Math.Clamp(((tile.Y + FootprintHeight) * TileSize - 1) / 10000f, 0f, 0.999f);
        batch.Draw(texture, destination, source, Color.White, 0f, Vector2.Zero, SpriteEffects.None, depth);
        return false;
    }
}
