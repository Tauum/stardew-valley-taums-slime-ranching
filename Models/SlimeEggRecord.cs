namespace taums_slime_ranching.Models;

/// <summary>Genome and hatch state for one unique Domesticated Slime Egg entity.</summary>
public sealed class SlimeEggRecord
{
    /// <summary>Stable per-egg token carried in the item's modData and in any machine job holding it.</summary>
    public string EggId { get; set; } = "";
    /// <summary>Fixed item data ID selected by the egg's color; different eggs of the same color still cannot stack.</summary>
    public string ItemId { get; set; } = "";
    public SlimeGenome Genome { get; set; } = new();
    /// <summary>Native animal ID once this genome egg has hatched; copied eggs can't hatch twice.</summary>
    public long? HatchedAnimalId { get; set; }
}
