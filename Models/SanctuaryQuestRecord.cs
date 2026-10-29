namespace taums_slime_ranching.Models;

/// <summary>Save-wide, idempotent milestones for the mandatory Sanctuary introduction.</summary>
public sealed class SanctuaryQuestRecord
{
    public HashSet<string> CompletedMilestones { get; set; } = new(StringComparer.Ordinal);
    public int? MaruRepairQuestAvailableOnDay { get; set; }
}
