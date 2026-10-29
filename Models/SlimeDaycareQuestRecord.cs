namespace taums_slime_ranching.Models;

/// <summary>Save-wide, idempotent milestones for the Slime Daycare quest.</summary>
public sealed class SlimeDaycareQuestRecord
{
    public HashSet<string> CompletedMilestones { get; set; } = new(StringComparer.Ordinal);
    public int? MaruRepairQuestAvailableOnDay { get; set; }
}
