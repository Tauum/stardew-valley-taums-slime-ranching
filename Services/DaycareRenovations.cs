using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.HomeRenovations;
using StardewValley.Menus;
using StardewValley.Objects;

namespace taums_slime_ranching.Services;

/// <summary>Scopes farmhouse-style room renovations to the Daycare AnimalHouse.</summary>
internal static class DaycareRenovations
{
    private const string StatePrefix = "Taum.SlimeRanching/DaycareRenovation/";
    private const string CellarParentKey = "Taum.SlimeRanching/DaycareCellarParent";
    private const string CellarOverrideKey = "daycare_cellar";

    private static readonly FieldInfo? AppliedOverridesField = typeof(GameLocation).GetField(
        "_appliedMapOverrides", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly (string Key, string Add, string Remove)[] RoomOverrides =
    {
        ("bedroom_open", "FarmHouse_Bedroom_Open", "FarmHouse_Bedroom_Normal"),
        ("southernroom_open", "FarmHouse_SouthernRoom_Add", "FarmHouse_SouthernRoom_Remove"),
        ("cornerroom_open", "FarmHouse_CornerRoom_Add", "FarmHouse_CornerRoom_Remove"),
        ("diningroom_open", "FarmHouse_DiningRoom_Add", "FarmHouse_DiningRoom_Remove"),
        ("cubby_open", "FarmHouse_Cubby_Add", "FarmHouse_Cubby_Remove"),
        ("farupperroom_open", "FarmHouse_FarUpperRoom_Add", "FarmHouse_FarUpperRoom_Remove"),
        ("extendedcorner_open", "FarmHouse_ExtendedCornerRoom_Add", "FarmHouse_ExtendedCornerRoom_Remove"),
        ("diningroomwall_open", "FarmHouse_DiningRoomWall_Add", "FarmHouse_DiningRoomWall_Remove")
    };

    internal static void Apply(AnimalHouse house)
    {
        if (!BuildingContent.IsSlimeDaycare(house.ParentBuilding?.buildingType.Value))
            return;

        if (GetHouseLevel(house) < 2)
            return;

        foreach ((string key, string add, string remove) in RoomOverrides)
        {
            if (key == "extendedcorner_open" && !IsEnabled(house, "corner_open"))
                continue;
            if (key == "diningroomwall_open" && !IsEnabled(house, "dining_open"))
                continue;
            ClearOverrideKey(house, key);
            house.ApplyMapOverride(IsEnabled(house, key) ? add : remove, key);
        }

        if (GetBuildingUpgradeTier(house) >= 3)
            ApplyCellar(house);
    }

    internal static void ShowMenu()
    {
        AnimalHouse? house = GetDaycareInterior();
        if (house is null || GetHouseLevel(house) < 2)
            return;

        List<ISalable> renovations = GetAvailableDaycareRenovations(house).Cast<ISalable>().ToList();
        if (renovations.Count == 0)
        {
            Game1.drawObjectDialogue("There are no Slime Daycare room renovations available right now.");
            return;
        }
        Game1.activeClickableMenu = new ShopMenu("HouseRenovations", renovations, 0, null, HouseRenovation.OnPurchaseRenovation)
        {
            purchaseSound = null
        };
    }

    private static IEnumerable<ISalable> GetAvailableDaycareRenovations(AnimalHouse house)
    {
        Dictionary<string, HomeRenovation> data = DataLoader.HomeRenovations(Game1.content);
        foreach ((string key, HomeRenovation value) in data)
        {
            // The Daycare intentionally has no crib. Never offer its removal or construction.
            if (string.Equals(value.SpecialRect, "crib", StringComparison.OrdinalIgnoreCase) || key.Contains("crib", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!MeetsRequirements(house, value.Requirements))
                continue;

            string[] text = Game1.content.LoadString(value.TextStrings).Split('/');
            var renovation = new DaycareRenovation
            {
                location = house,
                NameValue = key,
                DisplayNameValue = $"Daycare: {GetText(text, 0)}",
                DescriptionValue = GetText(text, 1),
                placementText = GetText(text, 2),
                Price = value.Price,
                RoomId = "Daycare_" + (string.IsNullOrEmpty(value.RoomId) ? key : value.RoomId),
                animationType = value.AnimationType == "destroy"
                    ? HouseRenovation.AnimationType.Destroy
                    : HouseRenovation.AnimationType.Build
            };

            if (!string.IsNullOrEmpty(value.SpecialRect))
                continue;

            foreach (RectGroup group in value.RectGroups)
            {
                var bounds = group.Rects.Select(rect => new Rectangle(rect.X, rect.Y, rect.Width, rect.Height)).ToList();
                renovation.AddRenovationBound(bounds);
            }

            if (value.CheckForObstructions)
                renovation.validate = HouseRenovation.EnsureNoObstructions;

            renovation.onRenovation = (_, selectedIndex) =>
            {
                foreach (RenovationValue action in value.RenovateActions)
                {
                    if (action.Type == "Mail")
                        house.modData[StatePrefix + action.Key] = action.Value;
                    else if (action.Type == "Value")
                        house.modData[StatePrefix + action.Key] = action.Value == "selected"
                            ? selectedIndex.ToString()
                            : action.Value;
                }
                Apply(house);
            };

            if (renovation.renovationBounds.Count > 0)
                yield return renovation;
        }
    }

    private static bool MeetsRequirements(AnimalHouse house, IEnumerable<RenovationValue> requirements)
    {
        foreach (RenovationValue requirement in requirements)
        {
            if (requirement.Type == "Mail")
            {
                bool expected = requirement.Value == "1";
                bool actual;
                if (requirement.Key.StartsWith("renovation_", StringComparison.Ordinal))
                {
                    string stateKey = StatePrefix + requirement.Key;
                    actual = house.modData.TryGetValue(stateKey, out string value) && value == "1";
                }
                else
                {
                    actual = Game1.player.hasOrWillReceiveMail(requirement.Key);
                }
                if (actual != expected)
                    return false;
            }
            else if (requirement.Type == "Value")
            {
                string raw = requirement.Value;
                bool equal = true;
                if (raw.StartsWith('!'))
                {
                    equal = false;
                    raw = raw[1..];
                }
                if (!int.TryParse(raw, out int expected))
                    return false;
                int actual = requirement.Key == "upgradeLevel"
                    ? GetHouseLevel(house)
                    : house.modData.TryGetValue(StatePrefix + requirement.Key, out string value) && int.TryParse(value, out int parsed)
                        ? parsed
                        : -1;
                bool matches = requirement.Key == "upgradeLevel" && equal
                    ? actual >= expected
                    : actual == expected;
                if (matches != equal)
                    return false;
            }
        }
        return true;
    }

    private static string GetText(string[] parts, int index) => index < parts.Length ? parts[index] : "Daycare renovation";

    private static bool IsEnabled(AnimalHouse house, string renovationKey)
    {
        string mailFlag = "renovation_" + renovationKey;
        return house.modData.TryGetValue(StatePrefix + mailFlag, out string value) && value == "1";
    }

    private static int GetHouseLevel(AnimalHouse house) => house.ParentBuilding?.buildingType.Value switch
    {
        BuildingContent.SlimeDaycareBuildingId => 1,
        BuildingContent.SlimeDaycareUpgrade1Id => 2,
        BuildingContent.SlimeDaycareUpgrade2Id => 2,
        BuildingContent.SlimeDaycareUpgrade3Id => 2,
        _ => 0
    };

    private static int GetBuildingUpgradeTier(AnimalHouse house) => house.ParentBuilding?.buildingType.Value switch
    {
        BuildingContent.SlimeDaycareUpgrade1Id => 1,
        BuildingContent.SlimeDaycareUpgrade2Id => 2,
        BuildingContent.SlimeDaycareUpgrade3Id => 3,
        _ => 0
    };

    private static AnimalHouse? GetDaycareInterior() => Game1.getFarm().buildings
        .FirstOrDefault(building => BuildingContent.IsSlimeDaycare(building.buildingType.Value))?
        .GetIndoors() as AnimalHouse;

    private static void ShowRenovationChoice()
    {
        Game1.currentLocation.createQuestionDialogue(
            "Which building would you like to renovate?",
            new[]
            {
                new Response("farmhouse", "Farmhouse Renovations"),
                new Response("daycare", "Slime Daycare Renovations"),
                new Response("cancel", "Leave")
            },
            "daycareRenovationMenu");
    }

    private static void ApplyCellar(AnimalHouse house)
    {
        ClearOverrideKey(house, CellarOverrideKey);
        house.ApplyMapOverride("FarmHouse_Cellar", CellarOverrideKey);

        string cellarName = GetCellarName(house);
        if (!Game1.locations.Any(location => location.Name == cellarName))
        {
            // Cellar's constructor automatically places 33 vanilla casks.
            // The Daycare uses the farmhouse cellar map and warp, but not its
            // wine-aging equipment.
            var cellar = new GameLocation("Maps\\Cellar", cellarName);
            cellar.modData[CellarParentKey] = house.NameOrUniqueName;
            Game1.locations.Add(cellar);
        }
        RemoveCellarKegs(house);

        var back = house.Map.GetLayer("Back");
        for (int x = 0; x < back.LayerWidth; x++)
        for (int y = 0; y < back.LayerHeight; y++)
        {
            string[] action = house.GetTilePropertySplitBySpaces("TouchAction", "Back", x, y);
            if (action.Length >= 2 && action[0] == "Warp" && action[1].StartsWith("Cellar", StringComparison.OrdinalIgnoreCase))
            {
                action[1] = cellarName;
                house.setTileProperty(x, y, "Back", "TouchAction", string.Join(" ", action));
            }
        }
        foreach (Warp warp in house.warps)
            if (warp.TargetName.StartsWith("Cellar", StringComparison.OrdinalIgnoreCase))
                warp.TargetName = cellarName;
    }

    private static string GetCellarName(AnimalHouse house) => house.NameOrUniqueName + "_Cellar";

    internal static int RemoveCellarKegs(AnimalHouse house)
    {
        string cellarName = GetCellarName(house);
        GameLocation? cellar = Game1.locations.FirstOrDefault(location => location.Name == cellarName);
        if (cellar is null)
            return 0;

        int removed = 0;
        foreach (var pair in cellar.objects.Pairs.ToArray())
        {
            if (pair.Value is not Cask && !pair.Value.Name.Contains("Cask", StringComparison.OrdinalIgnoreCase))
                continue;
            cellar.objects.Remove(pair.Key);
            removed++;
        }
        return removed;
    }

    private static void ClearOverrideKey(GameLocation location, string key)
    {
        if (AppliedOverridesField?.GetValue(location) is HashSet<string> keys)
            keys.Remove(key);
    }

    [HarmonyPatch(typeof(HouseRenovation), nameof(HouseRenovation.ShowRenovationMenu))]
    private static class ShowRenovationMenuPatch
    {
        private static bool Prefix()
        {
            AnimalHouse? daycare = GetDaycareInterior();
            if (daycare is null || GetHouseLevel(daycare) < 2)
                return true;

            ShowRenovationChoice();
            return false;
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.answerDialogueAction))]
    private static class RenovationChoicePatch
    {
        private static bool Prefix(string questionAndAnswer, ref bool __result)
        {
            if (questionAndAnswer == "daycareRenovationMenu_daycare")
            {
                ShowMenu();
                __result = true;
                return false;
            }

            if (questionAndAnswer == "daycareRenovationMenu_farmhouse")
            {
                Game1.activeClickableMenu = new ShopMenu(
                    "HouseRenovations",
                    HouseRenovation.GetAvailableRenovations(),
                    0,
                    null,
                    HouseRenovation.OnPurchaseRenovation)
                {
                    purchaseSound = null
                };
                __result = true;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.MakeMapModifications))]
    private static class MapReadyPatch
    {
        private static void Postfix(GameLocation __instance)
        {
            if (__instance is AnimalHouse house)
            {
                Apply(house);
                if (BuildingContent.IsSlimeDaycare(house.ParentBuilding?.buildingType.Value))
                    BuildingContent.RemoveFarmhouseOnlyFixtures(house);
            }
        }
    }

    [HarmonyPatch(typeof(GameLocation), "resetLocalState")]
    private static class CellarReturnWarpPatch
    {
        private static void Postfix(GameLocation __instance)
        {
            if (__instance.modData.TryGetValue(CellarParentKey, out string parent))
                foreach (Warp warp in __instance.warps)
                    warp.TargetName = parent;
        }
    }

    private sealed class DaycareRenovation : HouseRenovation
    {
        internal string NameValue { set => _name = value; }
        internal string DisplayNameValue { set => _displayName = value; }
        internal string DescriptionValue { set => _description = value; }
    }
}
