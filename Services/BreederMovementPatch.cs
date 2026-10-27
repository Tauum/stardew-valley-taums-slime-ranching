using System.Reflection;
using HarmonyLib;
using StardewValley;

namespace taums_slime_ranching.Services;

/// <summary>Prevents active breeder parents from choosing new random movement.</summary>
[HarmonyPatch]
internal static class BreederMovementPatch
{
    private static Func<long, bool>? isBreedingParentReserved;

    public static void SetReservationLookup(Func<long, bool> lookup) => isBreedingParentReserved = lookup;

    private static IEnumerable<MethodBase> TargetMethods()
    {
        MethodInfo? method = typeof(FarmAnimal).GetMethod("UpdateRandomMovements",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method is not null)
            yield return method;
    }

    private static bool Prefix(FarmAnimal __instance) =>
        isBreedingParentReserved?.Invoke(__instance.myID.Value) != true;
}

/// <summary>Hides active breeding parents while their sprites are represented in the breeder chambers.</summary>
[HarmonyPatch]
internal static class BreederParentVisibilityPatch
{
    private static Func<long, bool>? isInActiveBreedingCycle;

    public static void SetActiveCycleLookup(Func<long, bool> lookup) => isInActiveBreedingCycle = lookup;

    private static IEnumerable<MethodBase> TargetMethods() => typeof(FarmAnimal)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Where(method => method.Name == "draw"
            && method.GetParameters().FirstOrDefault()?.ParameterType == typeof(Microsoft.Xna.Framework.Graphics.SpriteBatch));

    private static bool Prefix(FarmAnimal __instance) =>
        isInActiveBreedingCycle?.Invoke(__instance.myID.Value) != true;
}
