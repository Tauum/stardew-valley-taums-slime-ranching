using StardewValley.Objects;

namespace taums_slime_ranching.Services;

/// <summary>Marks tester-spawned vanilla chests used to validate Egg CryoLab storage behavior.</summary>
internal static class EggCryoLabContent
{
    public const string ChestMarkerKey = "Taum.SlimeRanching/EggCryoLab";

    public static Chest CreateTestingChest()
    {
        var chest = new Chest(playerChest: true);
        chest.displayNameFormat = "Egg CryoLab";
        chest.modData[ChestMarkerKey] = "1";
        return chest;
    }

    public static bool IsEggCryoLab(Chest? chest) =>
        chest is not null && chest.modData.ContainsKey(ChestMarkerKey);
}
