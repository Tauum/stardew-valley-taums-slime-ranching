using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;

namespace taums_slime_ranching.Services;

/// <summary>Defines the seven breeder-slot color consumables.</summary>
internal static class ChromaticCrystalContent
{
    private static readonly (string Name, string Color, int SpriteIndex)[] Crystals =
    {
        ("Red", "#FF0000", 64),
        ("Orange", "#FF9500", 69),
        ("Yellow", "#FFD60A", 68),
        ("Green", "#00FF00", 60),
        ("Blue", "#0A84FF", 62),
        ("Purple", "#BF5AF2", 66),
        ("Pink", "#FF69B4", 872)
    };

    public static IReadOnlyList<string> QualifiedItemIds => Crystals
        .Select(crystal => "(O)" + GetItemId(crystal.Name))
        .ToArray();

    public static string GetItemId(string colorName) => $"Taum.SlimeRanching_ChromaticCrystal_{colorName}";

    public static bool TryGetColor(string qualifiedItemId, out string colorName, out string colorHex)
    {
        var crystal = Crystals.FirstOrDefault(item => "(O)" + GetItemId(item.Name) == qualifiedItemId);
        colorName = crystal.Name ?? "";
        colorHex = crystal.Color ?? "";
        return !string.IsNullOrEmpty(colorName);
    }

    public static bool IsColorName(string? colorName) =>
        colorName is not null && Crystals.Any(crystal => crystal.Name.Equals(colorName, StringComparison.OrdinalIgnoreCase));

    public static bool TryGetColorByName(string colorName, out string canonicalName, out string colorHex)
    {
        var crystal = Crystals.FirstOrDefault(item => item.Name.Equals(colorName, StringComparison.OrdinalIgnoreCase));
        canonicalName = crystal.Name ?? "";
        colorHex = crystal.Color ?? "";
        return canonicalName.Length > 0;
    }

    public static void EditAsset(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            return;

        e.Edit(asset =>
        {
            var objects = asset.AsDictionary<string, ObjectData>().Data;
            foreach (var crystal in Crystals)
            {
                string id = GetItemId(crystal.Name);
                objects.TryAdd(id, new ObjectData
                {
                    Name = $"{crystal.Name} Chromatic Crystal",
                    DisplayName = $"{crystal.Name} Chromatic Crystal",
                    Description = $"Consumed by a Slime Breeder to focus the offspring's color on {crystal.Name}.",
                    Type = "Basic",
                    Price = 0,
                    Texture = "Maps/springobjects",
                    SpriteIndex = crystal.SpriteIndex,
                    CanBeGivenAsGift = false,
                    CanBeTrashed = false,
                    ExcludeFromShippingCollection = true,
                    ContextTags = new List<string> { "taum_chromatic_crystal", $"taum_chromatic_crystal_{crystal.Name.ToLowerInvariant()}" }
                });
            }
        });
    }
}
