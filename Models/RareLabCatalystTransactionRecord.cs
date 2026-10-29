namespace taums_slime_ranching.Models;

/// <summary>Reconciles Gold Bars consumed to load a type-associated Rare Lab catalyst.</summary>
public sealed class RareLabCatalystTransactionRecord
{
    public string Id { get; set; } = string.Empty;
    public string MachineKey { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public string CatalystSpecies { get; set; } = string.Empty;
    public bool CatalystApplied { get; set; }
}
