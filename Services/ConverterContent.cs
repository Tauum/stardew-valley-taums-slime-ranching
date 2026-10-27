using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;

namespace taums_slime_ranching.Services;

/// <summary>Defines a distinct mod egg and a machine that accepts only vanilla slime eggs.</summary>
internal static class ConverterContent
{
    private const string CommonEggItemId = "Taum.SlimeRanching_CommonEgg";
    private const string ConverterItemId = "Taum.SlimeRanching_EggConverter";
    private const string BreederItemId = "Taum.SlimeRanching_SlimeBreeder";
    private const string IncubatorItemId = "Taum.SlimeRanching_SlimeIncubator";

    public const string CommonEggQualifiedId = "(O)" + CommonEggItemId;
    public const string ConverterQualifiedId = "(BC)" + ConverterItemId;
    public const string BreederQualifiedId = "(BC)" + BreederItemId;
    public const string IncubatorQualifiedId = "(BC)" + IncubatorItemId;

    private static readonly string[] VanillaEggIds =
    {
        "(O)680", // Green
        "(O)413", // Blue
        "(O)437", // Red
        "(O)439", // Purple
        "(O)857"  // Tiger
    };

    public static void EditAsset(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
        {
            e.Edit(asset =>
            {
                var objects = asset.AsDictionary<string, ObjectData>().Data;
                if (!objects.ContainsKey(CommonEggItemId))
                {
                    objects[CommonEggItemId] = new ObjectData
                    {
                        Name = "Common Slime Egg",
                        DisplayName = "Common Slime Egg",
                        Description = "A common Ranch Slime egg made by a converter. Hatches into a base Common Ranch Slime.",
                        Type = "Basic",
                        Price = 1000,
                        Texture = "Maps/springobjects",
                        SpriteIndex = 680,
                        CanBeGivenAsGift = false,
                        CanBeTrashed = true,
                        ExcludeFromShippingCollection = true,
                        ContextTags = new List<string> { "taum_common_slime_egg" }
                    };
                }
            });
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
        {
            e.Edit(asset =>
            {
                var craftables = asset.AsDictionary<string, BigCraftableData>().Data;
                if (!craftables.ContainsKey(ConverterItemId))
                {
                    craftables[ConverterItemId] = new BigCraftableData
                    {
                        Name = "Slime Egg Converter",
                        DisplayName = "Slime Egg Converter",
                        Description = "Turns one vanilla Slime Egg into one Common Slime Egg.",
                        CanBePlacedIndoors = true,
                        CanBePlacedOutdoors = true,
                        Texture = "TileSheets/Craftables",
                        SpriteIndex = 158
                    };
                }

                if (!craftables.ContainsKey(BreederItemId))
                {
                    craftables[BreederItemId] = new BigCraftableData
                    {
                        Name = "Slime Breeder",
                        DisplayName = "Slime Breeder",
                        Description = "Select two Ranch Slimes for breeding.",
                        CanBePlacedIndoors = true,
                        CanBePlacedOutdoors = false,
                        Texture = "TileSheets/Craftables",
                        SpriteIndex = 158
                    };
                }

                if (!craftables.ContainsKey(IncubatorItemId))
                {
                    craftables[IncubatorItemId] = new BigCraftableData
                    {
                        Name = "Slime Incubator",
                        DisplayName = "Slime Incubator",
                        Description = "Hatches a Common or genome-specific Ranch Slime Egg inside the Slime Daycare.",
                        CanBePlacedIndoors = true,
                        CanBePlacedOutdoors = false,
                        Texture = "TileSheets/Craftables",
                        SpriteIndex = 158
                    };
                }
            });
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
        {
            e.Edit(asset =>
            {
                var machines = asset.AsDictionary<string, MachineData>().Data;
                if (!machines.ContainsKey(ConverterQualifiedId))
                {
                    machines[ConverterQualifiedId] = new MachineData
                    {
                        // These are inferred from OutputRules, but setting them explicitly
                        // makes the converter's player-facing machine behavior unambiguous.
                        HasInput = true,
                        HasOutput = true,
                        OutputRules = new List<MachineOutputRule>
                        {
                            new()
                            {
                                Id = "ConvertVanillaSlimeEgg",
                                Triggers = VanillaEggIds.Select((id, index) => new MachineOutputTriggerRule
                                {
                                    Id = $"VanillaEgg{index}",
                                    Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                                    RequiredItemId = id,
                                    RequiredCount = 1
                                }).ToList(),
                                OutputItem = new List<MachineItemOutput>
                                {
                                    new() { ItemId = CommonEggQualifiedId, MinStack = 1, MaxStack = 1 }
                                },
                                MinutesUntilReady = 10
                            }
                        }
                    };
                }
            });
        }
    }
}
