using System.Text.Json;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;
using taums_slime_ranching.Models;

namespace taums_slime_ranching.Services;

/// <summary>Registers unique egg item IDs from the mod-folder external registries.</summary>
internal static class SlimeEggContent
{
    private const string RegistryFileName = "slime-registry-v1.json";

    public static string GetItemId(Guid genomeId) => $"Taum.SlimeRanching_Egg_{genomeId:N}";
    public static string GetQualifiedId(Guid genomeId) => "(O)" + GetItemId(genomeId);

    public static void EditAsset(AssetRequestedEventArgs e, string modDirectory)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            return;

        e.Edit(asset =>
        {
            var objects = asset.AsDictionary<string, ObjectData>().Data;
            foreach (SlimeEggRecord egg in LoadEggs(modDirectory))
            {
                if (objects.ContainsKey(egg.ItemId))
                    continue;

                SlimeGenome genome = egg.Genome;
                string color = DisplayOrUnknown(genome.VisualColor);
                objects[egg.ItemId] = new ObjectData
                {
                    Name = $"{DisplayOrUnknown(genome.PrimarySpecies)} Egg ({color})",
                    DisplayName = $"{DisplayOrUnknown(genome.PrimarySpecies)} Egg ({color})",
                    Description = CreateDescription(genome),
                    Type = "Basic",
                    Price = 0,
                    Texture = "Maps/springobjects",
                    SpriteIndex = 680,
                    CanBeGivenAsGift = false,
                    CanBeTrashed = true,
                    ExcludeFromShippingCollection = true,
                    ContextTags = new List<string> { "taum_ranch_slime_egg", $"taum_genome_{egg.Genome.Id:N}" }
                };
            }
        });
    }

    private static string CreateDescription(SlimeGenome genome)
    {
        string parents = genome.ParentA is Guid parentA && genome.ParentB is Guid parentB
            ? $"Parents: {parentA.ToString("N")[..8]} / {parentB.ToString("N")[..8]}"
            : "Parents: Unknown";
        string mutation = string.IsNullOrWhiteSpace(genome.MutationTrait) ? "None" : genome.MutationTrait;
        string likedFood = GetFoodName(genome.LikedFoodItemId);

        return $"A unique Ranch Slime Egg.\n"
            + $"Species: {DisplayOrUnknown(genome.PrimarySpecies)}"
            + (string.IsNullOrWhiteSpace(genome.SecondarySpecies) ? "" : $" / {genome.SecondarySpecies}") + "\n"
            + $"Color: {DisplayOrUnknown(genome.VisualColor)}\n"
            + $"Genetic size: {DisplayOrUnknown(genome.Size)}\n"
            + $"Rarity: {DisplayOrUnknown(genome.RarityTier)}{(genome.IsShiny ? " (Shiny)" : "")}\n"
            + $"Mutation: {mutation}\n"
            + $"Generation: {genome.Generation}\n"
            + $"{parents}\n"
            + $"Favorite food: {likedFood}";
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

        foreach (string path in Directory.EnumerateFiles(savesDirectory, RegistryFileName, SearchOption.AllDirectories))
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

            foreach (SlimeEggRecord egg in data?.Eggs ?? Enumerable.Empty<SlimeEggRecord>())
            {
                if (egg?.Genome is { Id: var id } && id != Guid.Empty && egg.ItemId == GetItemId(id))
                    yield return egg;
            }
        }
    }
}
