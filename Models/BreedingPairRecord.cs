namespace taums_slime_ranching.Models;

/// <summary>Externally stored parent reservation for one placed Slime Breeder.</summary>
public sealed class BreedingPairRecord
{
    public string MachineKey { get; set; } = "";
    public long ParentAId { get; set; }
    public long ParentBId { get; set; }
    public int CycleStartedOnDay { get; set; }
    public int CycleReadyOnDay { get; set; }
    /// <summary>Unique egg token, retained under the legacy property name for save migration.</summary>
    public string? OutputEggItemId { get; set; }
    /// <summary>Genome snapshot for output which has formed but hasn't been collected.</summary>
    public SlimeGenome? OutputEggGenome { get; set; }
    public string? ChromaticCrystalColor { get; set; }
    public bool ReadyNotificationSent { get; set; }
}
