namespace taums_slime_ranching.Models;

/// <summary>One external Slime Printer batch; its output stays reserved until collected.</summary>
public sealed class SlimePrinterJobRecord
{
    public string Id { get; set; } = string.Empty;
    public string MachineKey { get; set; } = "";
    public string InputItemId { get; set; } = "(O)170";
    public int InputCount { get; set; } = 1;
    public int SlimeFuelCount { get; set; } = 10;
    public string OutputItemId { get; set; } = "(O)338";
    public int OutputCount { get; set; } = 1;
    public int StartedOnDay { get; set; }
    public int ReadyOnDay { get; set; }
    public bool ReadyNotificationSent { get; set; }
}
