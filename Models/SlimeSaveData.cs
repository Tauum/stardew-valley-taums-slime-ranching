namespace taums_slime_ranching.Models;

public sealed class SlimeSaveData
{
    public const int CurrentSchemaVersion = 9;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public List<SlimeRecord> Slimes { get; set; } = new();
    public List<BreedingPairRecord> BreedingPairs { get; set; } = new();
    public List<SlimeEggRecord> Eggs { get; set; } = new();
    public List<IncubationRecord> Incubations { get; set; } = new();
    public List<MutagenicAttemptRecord> MutagenicAttempts { get; set; } = new();
}
