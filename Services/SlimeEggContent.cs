using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using taums_slime_ranching.Models;

namespace taums_slime_ranching.Services;

/// <summary>Registers stable colored egg item types; each item's unique genome lives in its modData.</summary>
internal static class SlimeEggContent
{
    private const string RegistryFileName = "slime-registry-v1.json";
    private const string LegacyEggItemPrefix = "Taum.SlimeRanching_Egg_";
    public const string EggIdModDataKey = "Taum.SlimeRanching/egg-id";
    public const string EggRecordModDataKey = "Taum.SlimeRanching/egg-record";
    private const string EggTexturePrefix = "Mods/Taum.SlimeRanching/Objects/RanchSlimeEgg_";
    private static readonly string[] EggColors = { "Red", "Orange", "Yellow", "Green", "Blue", "Purple", "Pink" };
    private static string? cachedModDirectory;
    private static List<SlimeEggRecord>? cachedEggs;
    private static Color[]? springObjectPixels;
    private static int springObjectPixelWidth;

    public static void InvalidateCache()
    {
        cachedModDirectory = null;
        cachedEggs = null;
    }

    public static void LoadAssets(AssetRequestedEventArgs e)
    {
        string assetName = e.NameWithoutLocale.BaseName;
        if (!assetName.StartsWith(EggTexturePrefix, StringComparison.Ordinal))
            return;

        string colorName = assetName[EggTexturePrefix.Length..];
        if (!new[] { "Red", "Orange", "Yellow", "Green", "Blue", "Purple", "Pink" }.Contains(colorName))
            return;

        Color tint = FarmAnimalContent.GetTintForVariantName(colorName);
        e.LoadFrom(() => CreateEggIcon(Game1.graphics.GraphicsDevice, tint), AssetLoadPriority.Exclusive);
    }

    public static string GetItemId(SlimeGenome genome) => $"Taum.SlimeRanching_Egg_{FarmAnimalContent.GetClosestVariantName(genome.VisualColor)}";
    public static string GetQualifiedId(SlimeGenome genome) => "(O)" + GetItemId(genome);
    public static string GetLegacyItemId(Guid genomeId) => $"{LegacyEggItemPrefix}{genomeId:N}";

    public static bool TryGetEggId(Item item, out string eggId)
    {
        if (item.modData.TryGetValue(EggIdModDataKey, out string? storedId)
            && Guid.TryParseExact(storedId, "N", out Guid storedGuid) && storedGuid != Guid.Empty)
        {
            eggId = storedGuid.ToString("N");
            return true;
        }

        string itemId = item.ItemId;
        if (itemId.StartsWith(LegacyEggItemPrefix, StringComparison.Ordinal)
            && Guid.TryParseExact(itemId[LegacyEggItemPrefix.Length..], "N", out Guid legacyGuid)
            && legacyGuid != Guid.Empty)
        {
            eggId = legacyGuid.ToString("N");
            return true;
        }

        eggId = "";
        return false;
    }

    public static bool TryReadRecord(Item item, out SlimeEggRecord egg)
    {
        egg = null!;
        if (!item.modData.TryGetValue(EggRecordModDataKey, out string? serialized)
            || string.IsNullOrWhiteSpace(serialized))
            return false;

        try
        {
            SlimeEggRecord? parsed = JsonSerializer.Deserialize<SlimeEggRecord>(serialized);
            if (parsed?.Genome is not { Id: var genomeId } || genomeId == Guid.Empty
                || !TryGetEggId(item, out string eggId)
                || !string.Equals(eggId, parsed.EggId, StringComparison.Ordinal)
                || !string.Equals(eggId, genomeId.ToString("N"), StringComparison.Ordinal))
                return false;

            parsed.ItemId = GetItemId(parsed.Genome);
            egg = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static void AttachRecord(Item item, SlimeEggRecord egg)
    {
        item.modData[EggIdModDataKey] = egg.EggId;
        item.modData[EggRecordModDataKey] = JsonSerializer.Serialize(egg);
        if (item is StardewValley.Object obj)
            obj.displayNameFormat = $"{DisplayOrUnknown(egg.Genome.PrimarySpecies)} Egg";
    }

    public static Item CreateItem(SlimeEggRecord egg)
    {
        Item item = ItemRegistry.Create(GetQualifiedId(egg.Genome));
        AttachRecord(item, egg);
        return item;
    }

    public static void EditAsset(AssetRequestedEventArgs e, string modDirectory)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            return;

        e.Edit(asset =>
        {
            var objects = asset.AsDictionary<string, ObjectData>().Data;
            foreach (string color in EggColors)
            {
                string itemId = $"Taum.SlimeRanching_Egg_{color}";
                objects[itemId] = new ObjectData
                {
                    Name = $"{color} Ranch Slime Egg",
                    DisplayName = $"{color} Ranch Slime Egg",
                    Description = "- Place in a Slime Incubator to hatch.\nA unique Ranch Slime egg. Its genome is carried with the item.",
                    Type = "Basic",
                    Price = 0,
                    Texture = EggTexturePrefix + color,
                    SpriteIndex = 0,
                    CanBeGivenAsGift = false,
                    CanBeTrashed = true,
                    ExcludeFromShippingCollection = true,
                    ContextTags = new List<string> { "taum_ranch_slime_egg", $"taum_ranch_slime_egg_color_{color.ToLowerInvariant()}" }
                };
            }

            // Keep old genome-specific IDs resolvable while existing saves migrate their egg items.
            foreach (SlimeEggRecord egg in GetEggs(modDirectory))
            {
                if (objects.ContainsKey(egg.ItemId))
                    continue;

                SlimeGenome genome = egg.Genome;
                string texture = EggTexturePrefix + FarmAnimalContent.GetClosestVariantName(genome.VisualColor);
                objects[egg.ItemId] = new ObjectData
                {
                    Name = $"{DisplayOrUnknown(genome.PrimarySpecies)} Egg",
                    DisplayName = $"{DisplayOrUnknown(genome.PrimarySpecies)} Egg",
                    Description = CreateDescription(genome, egg.HatchedAnimalId),
                    Type = "Basic",
                    Price = 0,
                    Texture = texture,
                    SpriteIndex = 0,
                    CanBeGivenAsGift = false,
                    CanBeTrashed = true,
                    ExcludeFromShippingCollection = true,
                    ContextTags = new List<string> { "taum_ranch_slime_egg", $"taum_genome_{egg.Genome.Id:N}" }
                };
            }
        });
    }

    private static Texture2D CreateEggIcon(GraphicsDevice graphicsDevice, Color tint)
    {
        const int tileSize = 16;
        const int vanillaEggSpriteIndex = 680;
        Color[] sourcePixels = GetSpringObjectPixels(out int sourceWidth);
        int columns = sourceWidth / tileSize;

        int sourceX = vanillaEggSpriteIndex % columns * tileSize;
        int sourceY = vanillaEggSpriteIndex / columns * tileSize;
        var texture = new Texture2D(graphicsDevice, tileSize, tileSize);
        var output = new Color[tileSize * tileSize];
        for (int y = 0; y < tileSize; y++)
        for (int x = 0; x < tileSize; x++)
        {
            Color pixel = sourcePixels[(sourceY + y) * sourceWidth + sourceX + x];
            if (pixel.A == 0)
                continue;

            float brightness = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B)) / 255f;
            output[y * tileSize + x] = new Color(
                (byte)(tint.R * brightness),
                (byte)(tint.G * brightness),
                (byte)(tint.B * brightness),
                pixel.A);
        }

        texture.SetData(output);
        return texture;
    }

    private static Color[] GetSpringObjectPixels(out int sourceWidth)
    {
        Texture2D source = Game1.content.Load<Texture2D>("Maps/springobjects");
        sourceWidth = source.Width;
        if (springObjectPixels is null || springObjectPixelWidth != source.Width
            || springObjectPixels.Length != source.Width * source.Height)
        {
            springObjectPixels = new Color[source.Width * source.Height];
            source.GetData(springObjectPixels);
            springObjectPixelWidth = source.Width;
        }

        return springObjectPixels;
    }

    internal static string CreateDescription(SlimeGenome genome, long? hatchedAnimalId)
    {
        string parents = genome.ParentA is Guid && genome.ParentB is Guid
            ? $"Parents: {DisplayOrUnknown(genome.ParentAName)} / {DisplayOrUnknown(genome.ParentBName)}"
            : "Parents: Unknown";
        string mutation = string.IsNullOrWhiteSpace(genome.MutationTrait) ? "None" : genome.MutationTrait;
        string likedFood = GetFoodName(genome.LikedFoodItemId);

        string hatchStatus = hatchedAnimalId is long
            ? "\nThis egg has already hatched."
            : "";
        return $"- Place in a Slime Incubator to hatch.\n"
            + $"A unique Ranch Slime Egg.\n"
            + $"Species: {DisplayOrUnknown(genome.PrimarySpecies)}"
            + (string.IsNullOrWhiteSpace(genome.SecondarySpecies) ? "" : $" / {genome.SecondarySpecies}") + "\n"
            + $"Color: {FarmAnimalContent.GetClosestVariantName(genome.VisualColor)}\n"
            + $"Genetic size: {DisplayOrUnknown(genome.Size)}\n"
            + $"Rarity: {DisplayOrUnknown(genome.RarityTier)}{(genome.IsShiny ? " (Shiny)" : "")}\n"
            + $"Mutation: {mutation}\n"
            + $"Generation: {genome.Generation}\n"
            + $"{parents}\n"
            + $"Favorite food: {likedFood}"
            + hatchStatus;
    }

    private static string DisplayOrUnknown(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Unknown" : value;

    private static string GetFoodName(string? qualifiedItemId) => qualifiedItemId switch
    {
        "(O)684" => "Bug Meat",
        "(O)766" => "Slime",
        "(O)767" => "Bat Wing",
        "(O)768" => "Solar Essence",
        "(O)769" => "Void Essence",
        "(O)167" => "Joja Cola",
        "(O)168" => "Trash",
        "(O)169" => "Driftwood",
        "(O)170" => "Broken Glasses",
        "(O)171" => "Broken CD",
        "(O)172" => "Soggy Newspaper",
        _ => "Unknown"
    };

    private static IEnumerable<SlimeEggRecord> LoadEggs(string modDirectory)
    {
        string savesDirectory = Path.Combine(modDirectory, "data", "saves");
        if (!Directory.Exists(savesDirectory))
            yield break;

        string[] registryPaths;
        try
        {
            registryPaths = Directory.EnumerateFiles(savesDirectory, RegistryFileName, SearchOption.AllDirectories).ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (string path in registryPaths)
        {
            SlimeSaveData? data;
            try
            {
                using FileStream stream = File.OpenRead(path);
                data = JsonSerializer.Deserialize<SlimeSaveData>(stream);
            }
            catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
            {
                continue;
            }

            Dictionary<Guid, string> parentNames = (data?.Slimes ?? new List<SlimeRecord>())
                .Where(record => !string.IsNullOrWhiteSpace(record.AnimalName))
                .GroupBy(record => record.Genome.Id)
                .ToDictionary(group => group.Key, group => group.First().AnimalName!);
            foreach (SlimeEggRecord egg in data?.Eggs ?? Enumerable.Empty<SlimeEggRecord>())
            {
                if (egg?.Genome is not { Id: var id } genome || id == Guid.Empty || egg.ItemId != GetLegacyItemId(id))
                    continue;

                if (genome.ParentA is Guid parentA && string.IsNullOrWhiteSpace(genome.ParentAName)
                    && parentNames.TryGetValue(parentA, out string? parentAName))
                    genome.ParentAName = parentAName;
                if (genome.ParentB is Guid parentB && string.IsNullOrWhiteSpace(genome.ParentBName)
                    && parentNames.TryGetValue(parentB, out string? parentBName))
                    genome.ParentBName = parentBName;
                yield return egg;
            }
        }
    }

    private static IReadOnlyList<SlimeEggRecord> GetEggs(string modDirectory)
    {
        if (cachedEggs is not null && string.Equals(cachedModDirectory, modDirectory, StringComparison.OrdinalIgnoreCase))
            return cachedEggs;

        cachedModDirectory = modDirectory;
        cachedEggs = LoadEggs(modDirectory).ToList();
        return cachedEggs;
    }
}
