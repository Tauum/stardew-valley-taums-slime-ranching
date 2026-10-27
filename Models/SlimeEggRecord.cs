namespace taums_slime_ranching.Models;

/// <summary>External genome identity for one genome-specific Ranch Slime Egg item ID.</summary>
public sealed class SlimeEggRecord
{
    public string ItemId { get; set; } = "";
    public SlimeGenome Genome { get; set; } = new();
    /// <summary>Native animal ID once this genome egg has hatched; copied eggs can't hatch twice.</summary>
    public long? HatchedAnimalId { get; set; }
}
