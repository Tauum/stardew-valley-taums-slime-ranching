namespace taums_slime_ranching.Models;

/// <summary>Externally stored hatch job keyed to one incubator in the Slime Daycare.</summary>
public sealed class IncubationRecord
{
    public string MachineKey { get; set; } = "";
    public string EggItemId { get; set; } = "";
    public int StartedOnDay { get; set; }
    public int ReadyOnDay { get; set; }
    /// <summary>Reserved native animal ID used to recover safely if game and mod state save at different moments.</summary>
    public long? HatchAnimalId { get; set; }
}
