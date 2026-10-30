using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace taums_slime_ranching.Services;

/// <summary>Draws the supplied Slime Breeder art at its saved world tile and sort depth.</summary>
[HarmonyPatch]
internal static class BreederDrawPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(StardewValley.Object)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Where(method => method.Name == "draw"
            && method.GetParameters().FirstOrDefault()?.ParameterType == typeof(SpriteBatch)
            && method.GetParameters().Count(parameter => parameter.ParameterType == typeof(int)) >= 2);

    private static bool Prefix(StardewValley.Object __instance, object[] __args)
    {
        if (__instance.QualifiedItemId != RanchingItemContent.BreederQualifiedId)
            return true;

        SpriteBatch batch = __args.OfType<SpriteBatch>().First();
        float alpha = __args.OfType<float>().DefaultIfEmpty(1f).First();
        MachineArtContent.DrawWorld(batch, __instance, __instance.TileLocation, alpha);
        return false;
    }
}
