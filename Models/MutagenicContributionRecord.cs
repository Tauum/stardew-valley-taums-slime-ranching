namespace taums_slime_ranching.Models;

/// <summary>Reconciles consumed Slime with a saved Mutagenic Pool chance increase.</summary>
public sealed class MutagenicContributionRecord
{
    public string Id { get; set; } = string.Empty;
    public string MachineKey { get; set; } = string.Empty;
    public long SlimeId { get; set; }
    public long Sequence { get; set; }
    public double PreviousContribution { get; set; }
    public bool ContributionApplied { get; set; }
}
