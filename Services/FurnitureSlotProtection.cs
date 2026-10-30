using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using taums_slime_ranching.Models;

namespace taums_slime_ranching.Services;

/// <summary>Reconciles Stardew's native fragility with saved Daycare furniture-slot assignments.</summary>
internal static class FurnitureSlotProtection
{
    public static bool Apply(AnimalHouse? daycare, SlimeRegistry registry)
    {
        if (daycare?.ParentBuilding is not Building building
            || !BuildingContent.IsSlimeDaycare(building.buildingType.Value))
            return false;

        string buildingId = building.id.Value.ToString("N");
        int buildingTier = BuildingContent.GetUpgradeTier(building.buildingType.Value);
        var protectedSlots = new Dictionary<Vector2, string>();
        foreach (DaycareFurnitureSlotRecord assignment in registry.GetFurnitureSlots(buildingId))
        {
            if (!FurnitureSlotCatalog.TryGetSlot(assignment.SlotId, out DaycareFurnitureSlotDefinition slot)
                || slot.RequiredBuildingTier > buildingTier
                || !FurnitureSlotCatalog.TryGetFurniture(assignment.FurnitureQualifiedItemId, out DaycareFurnitureRecipeListing furniture)
                || furniture.RequiredBuildingTier > buildingTier)
                continue;

            protectedSlots[new Vector2(assignment.TileX, assignment.TileY)] = assignment.FurnitureQualifiedItemId;
        }

        bool changed = false;
        foreach (StardewValley.Object placed in daycare.Objects.Values)
        {
            bool assignedFurniture = protectedSlots.TryGetValue(placed.TileLocation, out string? expectedItemId)
                && placed.QualifiedItemId == expectedItemId;
            if (assignedFurniture && placed.fragility.Value != StardewValley.Object.fragility_Indestructable)
            {
                placed.fragility.Value = StardewValley.Object.fragility_Indestructable;
                changed = true;
            }
            else if (!assignedFurniture
                && placed.fragility.Value == StardewValley.Object.fragility_Indestructable
                && FurnitureSlotCatalog.TryGetFurniture(placed.QualifiedItemId, out _))
            {
                // Every catalog furniture definition uses removable fragility by
                // default. Restore that value when the saved slot no longer owns it.
                placed.fragility.Value = StardewValley.Object.fragility_Removable;
                changed = true;
            }
        }

        return changed;
    }
}
