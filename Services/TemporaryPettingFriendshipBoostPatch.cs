using System.Reflection;
using HarmonyLib;
using StardewValley;

namespace taums_slime_ranching.Services;

/// <summary>Temporary test aid: make Ranch Slime friendship progress visible after one pet.</summary>
[HarmonyPatch]
internal static class TemporaryPettingFriendshipBoostPatch
{
    private const int TemporaryFriendshipPointsPerPet = 200;
    private const int MaximumFriendshipPoints = 1000;

    private static MethodBase? TargetMethod() => typeof(FarmAnimal)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Where(method => method.Name == "pet")
        .OrderBy(method => method.GetParameters().Length)
        .FirstOrDefault(method => method.GetParameters().FirstOrDefault()?.ParameterType == typeof(Farmer));

    private static void Prefix(FarmAnimal __instance, out int __state) =>
        __state = __instance.friendshipTowardFarmer.Value;

    private static void Postfix(FarmAnimal __instance, int __state)
    {
        if (__instance.type.Value != FarmAnimalContent.RanchSlimeTypeId)
            return;

        int currentPoints = __instance.friendshipTowardFarmer.Value;
        int vanillaIncrease = currentPoints - __state;
        if (vanillaIncrease <= 0)
            return;

        // Stardew adds its normal petting points first; temporarily raise the total
        // increase to 200 so one successful pet is visible as a full animal heart.
        int extraPoints = Math.Max(0, TemporaryFriendshipPointsPerPet - vanillaIncrease);
        __instance.friendshipTowardFarmer.Value = Math.Min(MaximumFriendshipPoints, currentPoints + extraPoints);
    }
}
