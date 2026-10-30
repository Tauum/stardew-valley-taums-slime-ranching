using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using taums_slime_ranching.Models;

namespace taums_slime_ranching.Services;

/// <summary>Applies Stardew's native indestructible fragility only to furniture recorded in Daycare slots.</summary>
internal static class FurnitureSlotProtection
{
    public static bool Apply(AnimalHouse? daycare, SlimeRegistry registry)
    {
        if (daycare?.ParentBuilding is not Building building
            || !BuildingContent.IsSlimeDaycare(building.buildingType.Value))
            return false;

        string buildingId = building.id.Value.ToString("N");
        int buildingTier = BuildingContent.GetUpgradeTier(building.buildingType.Value);
        bool changed = false;
        foreach (DaycareFurnitureSlotRecord assignment in registry.GetFurnitureSlots(buildingId))
        {
            if (!FurnitureSlotCatalog.TryGetSlot(assignment.SlotId, out DaycareFurnitureSlotDefinition slot)
                || slot.RequiredBuildingTier > buildingTier
                || !FurnitureSlotCatalog.TryGetFurniture(assignment.FurnitureQualifiedItemId, out DaycareFurnitureRecipeListing furniture)
                || furniture.RequiredBuildingTier > buildingTier)
                continue;

            Vector2 tile = new(assignment.TileX, assignment.TileY);
            if (!daycare.Objects.TryGetValue(tile, out StardewValley.Object? placed)
                || placed.QualifiedItemId != assignment.FurnitureQualifiedItemId
                || placed.fragility.Value == StardewValley.Object.fragility_Indestructable)
                continue;

            placed.fragility.Value = StardewValley.Object.fragility_Indestructable;
            changed = true;
        }

        return changed;
    }
}
