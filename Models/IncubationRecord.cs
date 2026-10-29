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
    /// <summary>Whether this job's Rare Lab outcome has been fixed before native hatch creation.</summary>
    public bool RareOutcomeResolved { get; set; }
    /// <summary>Whether the resolved outcome produces a rare species.</summary>
    public bool RareOffspringProduced { get; set; }
    /// <summary>New individual genome for a rare result; null for the egg's original genome.</summary>
    public SlimeGenome? HatchGenome { get; set; }
}
