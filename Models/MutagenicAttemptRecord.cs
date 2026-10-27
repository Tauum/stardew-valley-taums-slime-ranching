namespace taums_slime_ranching.Models;

/// <summary>External one-day Mutagenic Pool job; the stored roll prevents reload rerolls.</summary>
public sealed class MutagenicAttemptRecord
{
    public string MachineKey { get; set; } = "";
    public long SlimeId { get; set; }
    public int StartedOnDay { get; set; }
    public int ReadyOnDay { get; set; }
    public double ChanceAtStart { get; set; }
    public double Roll { get; set; }
}
