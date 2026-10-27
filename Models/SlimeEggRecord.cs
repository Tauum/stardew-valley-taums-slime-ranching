namespace taums_slime_ranching.Models;

/// <summary>External genome identity for one unique, non-stacking Ranch Slime Egg item ID.</summary>
public sealed class SlimeEggRecord
{
    public string ItemId { get; set; } = "";
    public SlimeGenome Genome { get; set; } = new();
}
