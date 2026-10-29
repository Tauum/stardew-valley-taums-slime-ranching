using HarmonyLib;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace taums_slime_ranching.Services;

internal static class SlimeDaycareQuestContent
{
    public const string MaruRepairQuestId = "Taum.SlimeRanching_MaruPoolRepair";
    public const string RepairDeliveryReceiptKey = "Taum.SlimeRanching/MaruRepairDeliveryReceipt";

    public static readonly (string ItemId, string Name, int Amount)[] MaruRepairMaterials =
    {
        ("(O)787", "Battery Pack", 1),
        ("(O)335", "Iron Bar", 5),
        ("(O)338", "Refined Quartz", 10)
    };

    private static Func<bool>? deliveryHandler;
    private static Func<bool>? promptHandler;

    public static void SetDeliveryHandler(Func<bool> handler) => deliveryHandler = handler;
    public static void SetPromptHandler(Func<bool> handler) => promptHandler = handler;

    public static void EditAsset(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Quests"))
            return;

        e.Edit(asset =>
        {
            var quests = asset.AsDictionary<string, string>().Data;
            quests[MaruRepairQuestId] = string.Join("/",
                "Basic",
                "Repair the Mutagenic Pool",
                "The Mutagenic Pool in the Slime Daycare is broken. Bring Maru 1 Battery Pack, 5 Iron Bars, and 10 Refined Quartz so she can repair it.",
                "Deliver the repair materials to Maru.",
                "",
                "",
                "0",
                "",
                "false");
        });
    }

    public static bool HasRepairMaterials(Farmer farmer, out string missing)
    {
        var missingItems = new List<string>();
        foreach (var material in MaruRepairMaterials)
        {
            int found = farmer.Items.Where(item => item?.QualifiedItemId == material.ItemId).Sum(item => item?.Stack ?? 0);
            if (found < material.Amount)
                missingItems.Add($"{material.Amount - found} {material.Name}{(material.Amount - found == 1 ? "" : "s")}");
        }

        missing = string.Join(", ", missingItems);
        return missingItems.Count == 0;
    }

    public static bool TryConsumeRepairMaterials(Farmer farmer, out List<InventorySnapshot> snapshots)
    {
        snapshots = new();
        if (!HasRepairMaterials(farmer, out _))
            return false;

        foreach (var material in MaruRepairMaterials)
        {
            int remaining = material.Amount;
            for (int slot = farmer.Items.Count - 1; slot >= 0 && remaining > 0; slot--)
            {
                Item? item = farmer.Items[slot];
                if (item?.QualifiedItemId != material.ItemId)
                    continue;

                int originalStack = item.Stack;
                int consumed = Math.Min(originalStack, remaining);
                snapshots.Add(new InventorySnapshot(slot, item, originalStack));
                item.Stack -= consumed;
                if (item.Stack <= 0)
                    farmer.Items[slot] = null;
                remaining -= consumed;
            }

            if (remaining > 0)
            {
                RestoreRepairMaterials(farmer, snapshots);
                snapshots.Clear();
                return false;
            }
        }

        return true;
    }

    public static void RestoreRepairMaterials(Farmer farmer, IEnumerable<InventorySnapshot> snapshots)
    {
        foreach (InventorySnapshot snapshot in snapshots)
        {
            snapshot.Item.Stack = snapshot.Stack;
            farmer.Items[snapshot.Slot] = snapshot.Item;
        }
    }

    internal sealed record InventorySnapshot(int Slot, Item Item, int Stack);

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.answerDialogueAction))]
    private static class MaruRepairQuestionPatch
    {
        private static bool Prefix(string questionAndAnswer, ref bool __result)
        {
            if (questionAndAnswer == "robinPoolRepairPrompt_later")
            {
                __result = true;
                return false;
            }

            if (questionAndAnswer == "robinPoolRepairPrompt_askMaru")
            {
                bool prompted = promptHandler?.Invoke() == true;
                if (!prompted)
                    Game1.drawObjectDialogue("Robin's advice could not be saved. Please ask her again.");
                __result = true;
                return false;
            }

            if (questionAndAnswer == "maruPoolRepairDelivery_later")
            {
                __result = true;
                return false;
            }

            if (questionAndAnswer != "maruPoolRepairDelivery_deliver")
                return true;

            bool delivered = deliveryHandler?.Invoke() == true;
            if (!delivered)
                Game1.drawObjectDialogue("The materials weren't delivered. They are still needed to repair the Pool.");
            __result = true;
            return false;
        }
    }
}
