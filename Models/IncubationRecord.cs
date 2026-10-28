namespace taums_slime_ranching.Models;

/// <summary>Externally stored hatch job keyed to one incubator in the Slime Daycare.</summary>
public sealed class IncubationRecord
{
    public string MachineKey { get; set; } = "";
    /// <summary>Unique egg token, retained under the legacy property name for save migration.</summary>
    public string EggItemId { get; set; } = "";
    public int StartedOnDay { get; set; }
    public int ReadyOnDay { get; set; }
    /// <summary>Reserved native animal ID used to recover safely if game and mod state save at different moments.</summary>
    public long? HatchAnimalId { get; set; }
    /// <summary>Genome snapshot for the egg while it is held by this machine.</summary>
    public SlimeGenome? EggGenome { get; set; }
}
