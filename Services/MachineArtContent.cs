using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace taums_slime_ranching.Services;

internal readonly record struct MachineFootprint(int Width, int Height);

/// <summary>Loads Taum's machine art and shares its approved placement geometry.</summary>
internal static class MachineArtContent
{
    public const string BreederTextureName = "Mods/Taum.SlimeRanching/Machines/SlimeBreeder";
    public const string IncubatorTextureName = "Mods/Taum.SlimeRanching/Machines/SlimeIncubator";
    public const string ResearchLabTextureName = "Mods/Taum.SlimeRanching/Machines/ResearchLab";
    public const string MutagenicPoolTextureName = "Mods/Taum.SlimeRanching/Machines/MutagenicPool";
    public const string EggCryoLabTextureName = "Mods/Taum.SlimeRanching/Machines/EggCryoLab";
    public const string SlimePrinterTextureName = "Mods/Taum.SlimeRanching/Machines/SlimePrinter";

    private static readonly (string AssetName, string FilePath)[] Assets =
    {
        (BreederTextureName, "assets/Machines/SlimeBreeder.png"),
        (IncubatorTextureName, "assets/Machines/SlimeIncubator.png"),
        (ResearchLabTextureName, "assets/Machines/ResearchLab.png"),
        (MutagenicPoolTextureName, "assets/Machines/MutagenicPool.png"),
        (EggCryoLabTextureName, "assets/Machines/EggCryoLab.png"),
        (SlimePrinterTextureName, "assets/Machines/SlimePrinter.png")
    };

    public static void LoadAssets(AssetRequestedEventArgs e)
    {
        foreach ((string assetName, string filePath) in Assets)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(assetName))
            {
                e.LoadFromModFile<Texture2D>(filePath, AssetLoadPriority.Medium);
                return;
            }
        }
    }

    public static Texture2D GetTexture(string assetName) => Game1.content.Load<Texture2D>(assetName);

    public static string? GetTextureName(StardewValley.Object machine) => machine.QualifiedItemId switch
    {
        var id when id == RanchingItemContent.BreederQualifiedId => BreederTextureName,
        var id when id == RanchingItemContent.IncubatorQualifiedId => IncubatorTextureName,
        var id when id == ResearchLabContent.QualifiedItemId => ResearchLabTextureName,
        var id when id == MutagenicPoolContent.QualifiedItemId => MutagenicPoolTextureName,
        var id when id == SlimePrinterContent.QualifiedItemId => SlimePrinterTextureName,
        _ when machine is Chest chest && EggCryoLabContent.IsEggCryoLab(chest) => EggCryoLabTextureName,
        _ => null
    };

    public static bool TryGetFootprint(StardewValley.Object machine, out MachineFootprint footprint)
    {
        footprint = machine.QualifiedItemId switch
        {
            var id when id == RanchingItemContent.BreederQualifiedId => new MachineFootprint(4, 2),
            var id when id == RanchingItemContent.IncubatorQualifiedId => new MachineFootprint(2, 3),
            var id when id == ResearchLabContent.QualifiedItemId => new MachineFootprint(4, 3),
            var id when id == MutagenicPoolContent.QualifiedItemId => new MachineFootprint(4, 2),
            var id when id == SlimePrinterContent.QualifiedItemId => new MachineFootprint(2, 3),
            _ when machine is Chest chest && EggCryoLabContent.IsEggCryoLab(chest) => new MachineFootprint(2, 3),
            _ => default
        };
        return footprint.Width > 0;
    }

    public static bool IsWithinFootprint(StardewValley.Object machine, Vector2 anchor, Vector2 tile)
    {
        if (!TryGetFootprint(machine, out MachineFootprint footprint))
            return false;

        return tile.X >= anchor.X && tile.X < anchor.X + footprint.Width
            && tile.Y >= anchor.Y && tile.Y < anchor.Y + footprint.Height;
    }

    public static bool TryGetMachineAt(GameLocation location, Vector2 tile, Func<StardewValley.Object, bool> predicate,
        out StardewValley.Object machine, out Vector2 anchor)
    {
        foreach (KeyValuePair<Vector2, StardewValley.Object> pair in location.Objects.Pairs)
        {
            if (predicate(pair.Value) && IsWithinFootprint(pair.Value, pair.Key, tile))
            {
                machine = pair.Value;
                anchor = pair.Key;
                return true;
            }
        }

        machine = null!;
        anchor = Vector2.Zero;
        return false;
    }

    public static Vector2 GetCenteredAnchor(Vector2 cursorTile, int width) =>
        new((int)cursorTile.X - width / 2, (int)cursorTile.Y);

    public static bool IsAreaClear(GameLocation location, Vector2 anchor, MachineFootprint footprint)
    {
        for (int y = 0; y < footprint.Height; y++)
        for (int x = 0; x < footprint.Width; x++)
        {
            Vector2 tile = anchor + new Vector2(x, y);
            if (!location.isTilePlaceable(tile, true)
                || location.Objects.ContainsKey(tile)
                || location.terrainFeatures.ContainsKey(tile))
                return false;
        }

        return true;
    }

    public static void DrawWorld(SpriteBatch batch, StardewValley.Object machine, Vector2 anchor, float alpha = 1f)
    {
        if (!TryGetFootprint(machine, out MachineFootprint footprint)
            || GetTextureName(machine) is not string textureName)
            return;

        Texture2D texture = GetTexture(textureName);
        const int tileSize = 64;
        int width = footprint.Width * tileSize;
        int height = footprint.Height * tileSize;
        Vector2 worldTopLeft = new(anchor.X * tileSize, anchor.Y * tileSize);
        Vector2 screenTopLeft = Game1.GlobalToLocal(Game1.viewport, worldTopLeft);
        Rectangle destination = new((int)screenTopLeft.X, (int)screenTopLeft.Y, width, height);
        float depth = Math.Clamp(((anchor.Y + footprint.Height) * tileSize - 8) / 10000f
            + anchor.X * 0.000001f, 0f, 0.999f);
        batch.Draw(texture, destination, texture.Bounds, Color.White * alpha, 0f, Vector2.Zero,
            SpriteEffects.None, depth);
    }

    public static void DrawPlacementAt(SpriteBatch batch, GameLocation location, StardewValley.Object machine,
        Vector2 anchor, float alpha = 0.5f)
    {
        if (!TryGetFootprint(machine, out MachineFootprint footprint))
            return;

        bool canPlace = IsAreaClear(location, anchor, footprint);
        Rectangle source = new(canPlace ? 194 : 210, 388, 16, 16);
        for (int y = 0; y < footprint.Height; y++)
        for (int x = 0; x < footprint.Width; x++)
        {
            Vector2 worldTile = (anchor + new Vector2(x, y)) * 64f;
            Vector2 screenTile = Game1.GlobalToLocal(Game1.viewport, worldTile);
            batch.Draw(Game1.mouseCursors, screenTile, source, Color.White, 0f, Vector2.Zero, 4f,
                SpriteEffects.None, 0.01f);
        }

        DrawWorld(batch, machine, anchor, alpha);
    }

    public static void DrawPlacementBounds(SpriteBatch batch, GameLocation location, StardewValley.Object machine)
    {
        if (!TryGetFootprint(machine, out MachineFootprint footprint))
            return;

        Vector2 grabTile = Game1.GetPlacementGrabTile();
        int pixelX = (int)grabTile.X * 64;
        int pixelY = (int)grabTile.Y * 64;
        Game1.isCheckingNonMousePlacement = !Game1.IsPerformingMousePlacement();
        if (Game1.isCheckingNonMousePlacement)
        {
            Vector2 nearby = Utility.GetNearbyValidPlacementPosition(Game1.player, location, machine, pixelX, pixelY);
            pixelX = (int)nearby.X;
            pixelY = (int)nearby.Y;
        }
        Game1.isCheckingNonMousePlacement = false;

        Vector2 cursorTile = new(pixelX / 64, pixelY / 64);
        if (machine.Equals(Game1.player.ActiveObject))
            machine.TileLocation = cursorTile;

        DrawPlacementAt(batch, location, machine, GetCenteredAnchor(cursorTile, footprint.Width));
    }

    public static bool TryPlace(StardewValley.Object machine, GameLocation location, ref int x, ref int y,
        ref bool result)
    {
        if (!TryGetFootprint(machine, out MachineFootprint footprint))
            return true;

        int cursorTileX = (int)Math.Floor(x / 64d);
        int cursorTileY = (int)Math.Floor(y / 64d);
        Vector2 anchor = GetCenteredAnchor(new Vector2(cursorTileX, cursorTileY), footprint.Width);
        if (!IsAreaClear(location, anchor, footprint))
        {
            result = false;
            return false;
        }

        x -= footprint.Width / 2 * 64;
        return true;
    }

    public static bool AreAdjacent(Vector2 firstAnchor, MachineFootprint first, Vector2 secondAnchor,
        MachineFootprint second)
    {
        bool horizontal = (firstAnchor.X + first.Width == secondAnchor.X || secondAnchor.X + second.Width == firstAnchor.X)
            && Math.Max(firstAnchor.Y, secondAnchor.Y) < Math.Min(firstAnchor.Y + first.Height, secondAnchor.Y + second.Height);
        bool vertical = (firstAnchor.Y + first.Height == secondAnchor.Y || secondAnchor.Y + second.Height == firstAnchor.Y)
            && Math.Max(firstAnchor.X, secondAnchor.X) < Math.Min(firstAnchor.X + first.Width, secondAnchor.X + second.Width);
        return horizontal || vertical;
    }
}

[HarmonyLib.HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.drawPlacementBounds),
    new[] { typeof(SpriteBatch), typeof(GameLocation) })]
internal static class EggCryoLabPlacementBoundsPatch
{
    private static bool Prefix(StardewValley.Object __instance, SpriteBatch spriteBatch, GameLocation location)
    {
        if (__instance is not Chest chest || !EggCryoLabContent.IsEggCryoLab(chest))
            return true;

        MachineArtContent.DrawPlacementBounds(spriteBatch, location, __instance);
        return false;
    }
}

[HarmonyLib.HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.placementAction),
    new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) })]
internal static class EggCryoLabPlacementActionPatch
{
    private static bool Prefix(StardewValley.Object __instance, GameLocation location, ref int x, ref int y,
        ref bool __result)
    {
        if (__instance is not Chest chest || !EggCryoLabContent.IsEggCryoLab(chest))
            return true;

        return MachineArtContent.TryPlace(__instance, location, ref x, ref y, ref __result);
    }
}

[HarmonyLib.HarmonyPatch(typeof(Chest), nameof(Chest.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) })]
internal static class EggCryoLabDrawPatch
{
    private static bool Prefix(Chest __instance, SpriteBatch spriteBatch, int x, int y, float alpha)
    {
        if (!EggCryoLabContent.IsEggCryoLab(__instance))
            return true;

        MachineArtContent.DrawWorld(spriteBatch, __instance, __instance.TileLocation, alpha);
        return false;
    }
}

internal static class EggCryoLabToolRemovalGeometry
{
    public static void RedirectHit(GameLocation location, ref int x, ref int y)
    {
        const int tileSize = 64;
        int tileX = x / tileSize;
        int tileY = y / tileSize;
        for (int anchorY = tileY - 2; anchorY <= tileY; anchorY++)
        for (int anchorX = tileX - 1; anchorX <= tileX; anchorX++)
        {
            Vector2 anchor = new(anchorX, anchorY);
            if (!location.Objects.TryGetValue(anchor, out StardewValley.Object? obj)
                || obj is not Chest chest
                || !EggCryoLabContent.IsEggCryoLab(chest)
                || !MachineArtContent.IsWithinFootprint(obj, anchor, new Vector2(tileX, tileY)))
                continue;

            x = anchorX * tileSize + tileSize / 2;
            y = anchorY * tileSize + tileSize / 2;
            return;
        }
    }
}

[HarmonyLib.HarmonyPatch(typeof(StardewValley.Tools.Axe), nameof(StardewValley.Tools.Axe.DoFunction),
    new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(int), typeof(Farmer) })]
internal static class EggCryoLabAxeRemovalPatch
{
    private static void Prefix(GameLocation location, ref int x, ref int y) =>
        EggCryoLabToolRemovalGeometry.RedirectHit(location, ref x, ref y);
}

[HarmonyLib.HarmonyPatch(typeof(StardewValley.Tools.Pickaxe), nameof(StardewValley.Tools.Pickaxe.DoFunction),
    new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(int), typeof(Farmer) })]
internal static class EggCryoLabPickaxeRemovalPatch
{
    private static void Prefix(GameLocation location, ref int x, ref int y) =>
        EggCryoLabToolRemovalGeometry.RedirectHit(location, ref x, ref y);
}

[HarmonyLib.HarmonyPatch]
internal static class MachineFacilityDrawPatch
{
    private static IEnumerable<System.Reflection.MethodBase> TargetMethods() => typeof(StardewValley.Object)
        .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
        .Where(method => method.Name == "draw"
            && method.GetParameters().FirstOrDefault()?.ParameterType == typeof(SpriteBatch)
            && method.GetParameters().Count(parameter => parameter.ParameterType == typeof(int)) >= 2);

    private static bool Prefix(StardewValley.Object __instance, object[] __args)
    {
        if (__instance.QualifiedItemId != ResearchLabContent.QualifiedItemId
            && __instance.QualifiedItemId != MutagenicPoolContent.QualifiedItemId
            && __instance.QualifiedItemId != SlimePrinterContent.QualifiedItemId)
            return true;

        SpriteBatch batch = __args.OfType<SpriteBatch>().First();
        MachineArtContent.DrawWorld(batch, __instance, __instance.TileLocation);
        return false;
    }
}
