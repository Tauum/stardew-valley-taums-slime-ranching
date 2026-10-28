using HarmonyLib;
using StardewValley;

namespace taums_slime_ranching.Services;

/// <summary>Vanilla ObjectData has no maximum-stack field, so constrain only genome-tagged eggs.</summary>
[HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.maximumStackSize))]
internal static class UniqueSlimeEggStackPatch
{
    private static bool Prefix(StardewValley.Object __instance, ref int __result)
    {
        if (!SlimeEggContent.TryGetEggId(__instance, out _))
            return true;

        __result = 1;
        return false;
    }
}
