namespace taums_slime_ranching.Models;

/// <summary>Mutable lifecycle data for one slime. The genome's ID is its stable identity.</summary>
public sealed class SlimeRecord
{
    public SlimeGenome Genome { get; set; } = new();
    /// <summary>The vanilla FarmAnimal ID used to match this mod record after save/load.</summary>
    public long? FarmAnimalId { get; set; }
    public string? FarmAnimalTypeId { get; set; }
    public string? AnimalName { get; set; }
    /// <summary>Authoritative maturity age in the external mod registry; mirrored to FarmAnimal.age for game behavior.</summary>
    public int AgeDays { get; set; }
    public int AgeLastUpdatedDay { get; set; }
    public bool AgeClockInitialized { get; set; }
    /// <summary>First in-game day when this slime may start another breeding cycle.</summary>
    public int BreedingCooldownUntilDay { get; set; }
    /// <summary>Failed Mutagenic Pool rolls, used by its per-slime pity chance.</summary>
    public int MutagenicPoolFailures { get; set; }
    /// <summary>Player-contributed Mutagenic Pool chance, in percentage points.</summary>
    public double MutagenicPoolContribution { get; set; }
    /// <summary>Last day this juvenile received a Compost Trough feeding; stored externally to limit feeding to once per day.</summary>
    public int LastCompostFeedDay { get; set; } = -1;
    /// <summary>Day and daily count for autonomous enrichment furniture use.</summary>
    public int EnrichmentActivityDay { get; set; } = -1;
    public int EnrichmentActivitiesToday { get; set; }
    /// <summary>Next in-game minute when this slime considers using nearby furniture.</summary>
    public int NextEnrichmentCheckMinute { get; set; } = -1;
    public string? LastHutchName { get; set; }
    public bool IsCompanion { get; set; }
}
