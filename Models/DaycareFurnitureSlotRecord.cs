namespace taums_slime_ranching.Models;

/// <summary>Saved furniture assignment and stable tile for one fixed Daycare slot.</summary>
public sealed class DaycareFurnitureSlotRecord
{
    public string DaycareBuildingId { get; set; } = string.Empty;
    public string SlotId { get; set; } = string.Empty;
    public string FurnitureQualifiedItemId { get; set; } = string.Empty;
    public int TileX { get; set; }
    public int TileY { get; set; }
}
