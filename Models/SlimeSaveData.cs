using System.Text.Json.Serialization;

namespace taums_slime_ranching.Models;

public sealed class SlimeSaveData
{
    public const int CurrentSchemaVersion = 25;

    public int SchemaVersion { get; set; }
    public List<SlimeRecord> Slimes { get; set; } = new();
    public List<BreedingPairRecord> BreedingPairs { get; set; } = new();
    public List<SlimeEggRecord> Eggs { get; set; } = new();
    public List<IncubationRecord> Incubations { get; set; } = new();
    public List<MutagenicAttemptRecord> MutagenicAttempts { get; set; } = new();
    [Newtonsoft.Json.JsonProperty("RefineryJobs")]
    [JsonPropertyName("RefineryJobs")]
    public List<SlimePrinterJobRecord> SlimePrinterJobs { get; set; } = new();
    public SlimeDaycareQuestRecord SlimeDaycareQuest { get; set; } = new();
    public long? ActiveCompanionFarmAnimalId { get; set; }
    public List<string> HatchedEggIds { get; set; } = new();
    public List<string> DestroyedEggIds { get; set; } = new();
    public List<IncubationRecord> PendingIncubationCancellations { get; set; } = new();
    public List<MutagenicContributionRecord> PendingMutagenicContributions { get; set; } = new();
    public List<RareLabRecord> RareLabs { get; set; } = new();
    public List<RareLabContributionRecord> PendingRareLabContributions { get; set; } = new();
    public List<RareLabCatalystTransactionRecord> PendingRareLabCatalystTransactions { get; set; } = new();
    [Newtonsoft.Json.JsonProperty("PendingRefineryTransactions")]
    [JsonPropertyName("PendingRefineryTransactions")]
    public List<SlimePrinterTransactionRecord> PendingSlimePrinterTransactions { get; set; } = new();
    public List<DaycareFurnitureSlotRecord> FurnitureSlots { get; set; } = new();
}
