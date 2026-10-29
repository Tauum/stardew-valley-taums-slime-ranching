namespace taums_slime_ranching.Models;

/// <summary>Persistent chance state for the Rare Lab paired with one incubator.</summary>
public sealed class RareLabRecord
{
    public string MachineKey { get; set; } = "";
    public bool IsUnlocked { get; set; }
    public int BaseChancePoints { get; set; }
    public int PityPoints { get; set; }
    public int ContributionPoints { get; set; }
}
