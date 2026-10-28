using HarmonyLib;
using StardewValley;

namespace taums_slime_ranching.Services;

/// <summary>Show the genome attached to this unique Ranch Slime Egg in its normal item tooltip.</summary>
[HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.getDescription))]
internal static class SlimeEggDescriptionPatch
{
    private static void Postfix(StardewValley.Object __instance, ref string __result)
    {
        if (!SlimeEggContent.TryReadRecord(__instance, out Models.SlimeEggRecord egg))
            return;

        string description = SlimeEggContent.CreateDescription(egg.Genome, egg.HatchedAnimalId);
        int tooltipWidth = Math.Max(272, (int)Game1.dialogueFont.MeasureString(__instance.DisplayName).X);
        __result = Game1.parseText(description, Game1.smallFont, tooltipWidth);
    }
}
