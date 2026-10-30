namespace taums_slime_ranching.Models;

public enum SlimePrinterTransactionKind
{
    Start,
    Collect
}

/// <summary>Reconciles one Slime Printer inventory change with its external job record.</summary>
public sealed class SlimePrinterTransactionRecord
{
    public string Id { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public SlimePrinterTransactionKind Kind { get; set; }
    public string MachineKey { get; set; } = string.Empty;
    public SlimePrinterJobRecord Job { get; set; } = new();
}
