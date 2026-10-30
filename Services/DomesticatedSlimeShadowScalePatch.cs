using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace taums_slime_ranching.Services;

/// <summary>Scales only a Domesticated Slime's native ground shadow to match its genome-sized sprite.</summary>
[HarmonyPatch]
internal static class DomesticatedSlimeShadowDrawContextPatch
{
    [ThreadStatic]
    private static FarmAnimal? drawingAnimal;

    public static FarmAnimal? DrawingAnimal => drawingAnimal;

    private static MethodBase TargetMethod() => AccessTools.Method(typeof(FarmAnimal), "draw", new[] { typeof(SpriteBatch) })
        ?? throw new MissingMethodException(typeof(FarmAnimal).FullName, "draw(SpriteBatch)");

    private static void Prefix(FarmAnimal __instance, out FarmAnimal? __state)
    {
        __state = drawingAnimal;
        drawingAnimal = __instance;
    }

    private static void Postfix(FarmAnimal? __state) => drawingAnimal = __state;
}

[HarmonyPatch]
internal static class DomesticatedSlimeShadowScalePatch
{
    private static Func<long, string?>? getGenomeSize;

    public static void SetSizeLookup(Func<long, string?> lookup) => getGenomeSize = lookup;

    private static IEnumerable<MethodBase> TargetMethods() => typeof(AnimatedSprite)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Where(method => method.Name == "drawShadow"
            && method.GetParameters() is ParameterInfo[] parameters
            && parameters.Length >= 3
            && parameters[0].ParameterType == typeof(SpriteBatch)
            && parameters[2].ParameterType == typeof(float));

    private static void Prefix(AnimatedSprite __instance, object[] __args)
    {
        FarmAnimal? animal = DomesticatedSlimeShadowDrawContextPatch.DrawingAnimal;
        if (animal is null
            || animal.type.Value != FarmAnimalContent.DomesticatedSlimeTypeId
            || !ReferenceEquals(animal.Sprite, __instance)
            || __args.Length < 3
            || __args[2] is not float shadowScale
            || getGenomeSize?.Invoke(animal.myID.Value) is not string size)
            return;

        __args[2] = shadowScale * FarmAnimalContent.GetSizeScale(size);
    }
}
