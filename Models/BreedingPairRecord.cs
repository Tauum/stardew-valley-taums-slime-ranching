namespace taums_slime_ranching.Models;

/// <summary>Externally stored parent reservation for one placed Slime Breeder.</summary>
public sealed class BreedingPairRecord
{
    public string MachineKey { get; set; } = "";
    public long ParentAId { get; set; }
    public long ParentBId { get; set; }
    public int CycleStartedOnDay { get; set; }
    public int CycleReadyOnDay { get; set; }
    public string? OutputEggItemId { get; set; }
}
