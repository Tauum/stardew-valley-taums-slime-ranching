namespace taums_slime_ranching.Models;

public sealed class SlimeSaveData
{
    public const int CurrentSchemaVersion = 24;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public List<SlimeRecord> Slimes { get; set; } = new();
    public List<BreedingPairRecord> BreedingPairs { get; set; } = new();
    public List<SlimeEggRecord> Eggs { get; set; } = new();
    public List<IncubationRecord> Incubations { get; set; } = new();
    public List<MutagenicAttemptRecord> MutagenicAttempts { get; set; } = new();
    public List<RefineryJobRecord> RefineryJobs { get; set; } = new();
    public SlimeDaycareQuestRecord SlimeDaycareQuest { get; set; } = new();
    public long? ActiveCompanionFarmAnimalId { get; set; }
    public List<string> HatchedEggIds { get; set; } = new();
    public List<string> DestroyedEggIds { get; set; } = new();
    public List<IncubationRecord> PendingIncubationCancellations { get; set; } = new();
    public List<MutagenicContributionRecord> PendingMutagenicContributions { get; set; } = new();
    public List<RareLabRecord> RareLabs { get; set; } = new();
    public List<RareLabContributionRecord> PendingRareLabContributions { get; set; } = new();
    public List<RareLabCatalystTransactionRecord> PendingRareLabCatalystTransactions { get; set; } = new();
    public List<RefineryTransactionRecord> PendingRefineryTransactions { get; set; } = new();
}
