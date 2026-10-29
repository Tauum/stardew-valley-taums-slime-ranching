namespace taums_slime_ranching.Models;

/// <summary>Reconciles Slime consumed for one incubator's Rare Lab contribution.</summary>
public sealed class RareLabContributionRecord
{
    public string Id { get; set; } = string.Empty;
    public string MachineKey { get; set; } = string.Empty;
    public string EggItemId { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public int PreviousContributionPoints { get; set; }
    public bool ContributionApplied { get; set; }
}
