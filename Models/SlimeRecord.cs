namespace taums_slime_ranching.Models;

/// <summary>Mutable lifecycle data for one slime. The genome's ID is its stable identity.</summary>
public sealed class SlimeRecord
{
    public SlimeGenome Genome { get; set; } = new();
    /// <summary>The vanilla FarmAnimal ID used to match this mod record after save/load.</summary>
    public long? FarmAnimalId { get; set; }
    public string? FarmAnimalTypeId { get; set; }
    /// <summary>Authoritative maturity age in the external mod registry; mirrored to FarmAnimal.age for game behavior.</summary>
    public int AgeDays { get; set; }
    public int AgeLastUpdatedDay { get; set; }
    public bool AgeClockInitialized { get; set; }
    /// <summary>First in-game day when this slime may start another breeding cycle.</summary>
    public int BreedingCooldownUntilDay { get; set; }
    public string? LastHutchName { get; set; }
    public bool IsCompanion { get; set; }
}
