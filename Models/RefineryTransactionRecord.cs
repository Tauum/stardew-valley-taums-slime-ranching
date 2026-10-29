namespace taums_slime_ranching.Models;

public enum RefineryTransactionKind
{
    Start,
    Collect
}

/// <summary>Reconciles one refinery inventory change with its external job record.</summary>
public sealed class RefineryTransactionRecord
{
    public string Id { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public RefineryTransactionKind Kind { get; set; }
    public string MachineKey { get; set; } = string.Empty;
    public RefineryJobRecord Job { get; set; } = new();
}
