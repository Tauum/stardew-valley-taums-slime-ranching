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
