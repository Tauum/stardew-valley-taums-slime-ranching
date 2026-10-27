namespace taums_slime_ranching.Models;

/// <summary>Mutable lifecycle data for one slime. The genome's ID is its stable identity.</summary>
public sealed class SlimeRecord
{
    public SlimeGenome Genome { get; set; } = new();
    /// <summary>The vanilla FarmAnimal ID used to match this mod record after save/load.</summary>
    public long? FarmAnimalId { get; set; }
    public string? FarmAnimalTypeId { get; set; }
    public int AgeDays { get; set; }
    public string? LastHutchName { get; set; }
    public bool IsCompanion { get; set; }
}
