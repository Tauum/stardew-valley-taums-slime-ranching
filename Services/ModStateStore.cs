using System.Text.Json;
using System.Text.Json.Nodes;
using System.IO.Compression;
using StardewModdingAPI;
using taums_slime_ranching.Models;

namespace taums_slime_ranching.Services;

/// <summary>Stores active state in SMAPI's per-save data and imports the previous mod-folder registry once.</summary>
internal sealed class ModStateStore
{
    private const string SaveDataKey = "slime-ranching-state";
    private const string RegistryFileName = "slime-registry-v1.json";
    private readonly IDataHelper data;
    private readonly string modDirectory;
    private string? registryPath;

    public int? LoadedSchemaVersion { get; private set; }
    public bool ImportedLegacyData { get; private set; }
    public bool MigratedSaveData { get; private set; }

    public ModStateStore(IDataHelper data, string modDirectory)
    {
        this.data = data;
        this.modDirectory = modDirectory;
    }

    public SlimeSaveData? Load()
    {
        this.ImportedLegacyData = false;
        this.MigratedSaveData = false;
        SlimeSaveData? saveData = this.data.ReadSaveData<SlimeSaveData>(SaveDataKey);
        if (saveData is not null)
        {
            this.LoadedSchemaVersion = saveData.SchemaVersion;
            if (saveData.SchemaVersion == 11)
            {
                foreach (SlimeEggRecord egg in saveData.Eggs)
                {
                    if (egg?.Genome is not { Id: var id } || id == Guid.Empty)
                        continue;

                    egg.EggId = id.ToString("N");
                    egg.ItemId = SlimeEggContent.GetItemId(egg.Genome);
                    if (egg.HatchedAnimalId is not null && !saveData.HatchedEggIds.Contains(egg.EggId, StringComparer.Ordinal))
                        saveData.HatchedEggIds.Add(egg.EggId);
                }
                foreach (BreedingPairRecord pair in saveData.BreedingPairs)
                {
                    if (pair.OutputEggItemId is not null)
                        pair.OutputEggItemId = NormalizeEggToken(pair.OutputEggItemId);
                }
                foreach (IncubationRecord incubation in saveData.Incubations)
                {
                    incubation.EggItemId = NormalizeEggToken(incubation.EggItemId);
                    if (incubation.EggGenome is null)
                        incubation.EggGenome = saveData.Eggs.FirstOrDefault(egg => egg.EggId == incubation.EggItemId)?.Genome;
                }
                saveData.SchemaVersion = 12;
                this.MigratedSaveData = true;
            }
            if (saveData.SchemaVersion == 12)
            {
                saveData.SlimeDaycareQuest ??= new SlimeDaycareQuestRecord();
                saveData.SchemaVersion = 13;
                this.MigratedSaveData = true;
            }
            if (saveData.SchemaVersion == 13)
            {
                saveData.SlimeDaycareQuest ??= new SlimeDaycareQuestRecord();
                saveData.SchemaVersion = 14;
                this.MigratedSaveData = true;
            }
            if (saveData.SchemaVersion == 14)
            {
                saveData.DestroyedEggIds ??= new List<string>();
                saveData.SchemaVersion = 15;
                this.MigratedSaveData = true;
            }
            if (saveData.SchemaVersion == 15)
            {
                saveData.PendingIncubationCancellations ??= new List<IncubationRecord>();
                saveData.SchemaVersion = 16;
                this.MigratedSaveData = true;
            }
            if (saveData.SchemaVersion == 16)
            {
                foreach (SlimeRecord? slime in saveData.Slimes)
                {
                    if (slime is not null)
                        slime.MutagenicPoolContribution = double.IsFinite(slime.MutagenicPoolContribution)
                            ? Math.Clamp(slime.MutagenicPoolContribution, 0, 1)
                            : 0;
                }
                saveData.SchemaVersion = 17;
                this.MigratedSaveData = true;
            }
            if (saveData.SchemaVersion == 17)
            {
                saveData.PendingMutagenicContributions ??= new List<MutagenicContributionRecord>();
                saveData.SchemaVersion = 18;
                this.MigratedSaveData = true;
            }
            if (saveData.SchemaVersion == 18)
            {
                saveData.RareLabs ??= new List<RareLabRecord>();
                saveData.SchemaVersion = 19;
                this.MigratedSaveData = true;
            }
            if (saveData.SchemaVersion == 19)
            {
                saveData.PendingRefineryTransactions ??= new List<RefineryTransactionRecord>();
                saveData.SchemaVersion = 20;
                this.MigratedSaveData = true;
            }
            if (saveData.SchemaVersion == 20)
            {
                saveData.RareLabs ??= new List<RareLabRecord>();
                saveData.PendingRefineryTransactions ??= new List<RefineryTransactionRecord>();
                saveData.PendingRareLabContributions ??= new List<RareLabContributionRecord>();
                saveData.SchemaVersion = 21;
                this.MigratedSaveData = true;
            }
            if (saveData.SchemaVersion == 21)
            {
                saveData.SchemaVersion = 22;
                this.MigratedSaveData = true;
            }
            if (saveData.SchemaVersion == 22)
            {
                saveData.PendingRareLabCatalystTransactions ??= new List<RareLabCatalystTransactionRecord>();
                saveData.SchemaVersion = 23;
                this.MigratedSaveData = true;
            }
            if (saveData.SchemaVersion == 23)
            {
                saveData.SlimeDaycareQuest ??= new SlimeDaycareQuestRecord();
                saveData.SchemaVersion = SlimeSaveData.CurrentSchemaVersion;
                this.MigratedSaveData = true;
            }
            return saveData;
        }

        return this.LoadLegacyExternalData();
    }

    private SlimeSaveData? LoadLegacyExternalData()
    {
        this.ImportedLegacyData = false;
        this.MigratedSaveData = false;
        this.LoadedSchemaVersion = null;
        this.registryPath = Path.Combine(GetSaveDirectory(this.modDirectory), RegistryFileName);
        if (!File.Exists(this.registryPath))
            return null;

        using var stream = File.OpenRead(this.registryPath);
        JsonNode? root = JsonNode.Parse(stream);
        if (root is JsonObject initialRoot && initialRoot["SchemaVersion"] is JsonValue schemaNode
            && schemaNode.TryGetValue(out int initialSchemaVersion))
            this.LoadedSchemaVersion = initialSchemaVersion;
        if (root is JsonObject rootObject && rootObject["SchemaVersion"]?.GetValue<int>() == 1)
        {
            rootObject["SchemaVersion"] = 2;
            rootObject["BreedingPairs"] = new JsonArray();
        }

        if (root is JsonObject schemaTwo && schemaTwo["SchemaVersion"]?.GetValue<int>() == 2)
        {
            schemaTwo["SchemaVersion"] = 3;
            schemaTwo["Eggs"] = new JsonArray();
        }

        if (root is JsonObject schemaThree && schemaThree["SchemaVersion"]?.GetValue<int>() == 3)
        {
            schemaThree["SchemaVersion"] = 4;
        }

        if (root is JsonObject schemaFour && schemaFour["SchemaVersion"]?.GetValue<int>() == 4)
        {
            schemaFour["SchemaVersion"] = 5;
        }

        if (root is JsonObject schemaFive && schemaFive["SchemaVersion"]?.GetValue<int>() == 5)
        {
            schemaFive["SchemaVersion"] = 6;
        }

        if (root is JsonObject schemaSix && schemaSix["SchemaVersion"]?.GetValue<int>() == 6)
        {
            schemaSix["SchemaVersion"] = 7;
        }

        if (root is JsonObject schemaSeven && schemaSeven["SchemaVersion"]?.GetValue<int>() == 7)
        {
            schemaSeven["SchemaVersion"] = 8;
        }

        if (root is JsonObject schemaEight && schemaEight["SchemaVersion"]?.GetValue<int>() == 8)
        {
            schemaEight["SchemaVersion"] = 9;
            schemaEight["MutagenicAttempts"] = new JsonArray();
        }

        if (root is JsonObject schemaNine && schemaNine["SchemaVersion"]?.GetValue<int>() == 9)
        {
            schemaNine["SchemaVersion"] = 10;
            schemaNine["RefineryJobs"] = new JsonArray();
        }

        if (root is JsonObject schemaTen && schemaTen["SchemaVersion"]?.GetValue<int>() == 10)
        {
            schemaTen["SchemaVersion"] = 11;
        }

        if (root is JsonObject schemaEleven && schemaEleven["SchemaVersion"]?.GetValue<int>() == 11)
        {
            schemaEleven["SchemaVersion"] = 12;
        }

        if (root is JsonObject schemaTwelve && schemaTwelve["SchemaVersion"]?.GetValue<int>() == 12)
        {
            schemaTwelve["SchemaVersion"] = 13;
        }

        if (root is JsonObject schemaThirteen && schemaThirteen["SchemaVersion"]?.GetValue<int>() == 13)
        {
            schemaThirteen["SchemaVersion"] = 14;
        }

        if (root is JsonObject schemaFourteen && schemaFourteen["SchemaVersion"]?.GetValue<int>() == 14)
        {
            schemaFourteen["SchemaVersion"] = 15;
            schemaFourteen["DestroyedEggIds"] = new JsonArray();
        }

        if (root is JsonObject schemaFifteen && schemaFifteen["SchemaVersion"]?.GetValue<int>() == 15)
        {
            schemaFifteen["SchemaVersion"] = 16;
            schemaFifteen["PendingIncubationCancellations"] = new JsonArray();
        }

        if (root is JsonObject schemaSixteen && schemaSixteen["SchemaVersion"]?.GetValue<int>() == 16)
        {
            schemaSixteen["SchemaVersion"] = 17;
            if (schemaSixteen["Slimes"] is JsonArray slimeRecords)
            {
                foreach (JsonNode? slimeNode in slimeRecords)
                {
                    if (slimeNode is JsonObject slime && !slime.ContainsKey("MutagenicPoolContribution"))
                        slime["MutagenicPoolContribution"] = 0;
                }
            }
        }

        if (root is JsonObject schemaSeventeen && schemaSeventeen["SchemaVersion"]?.GetValue<int>() == 17)
        {
            schemaSeventeen["SchemaVersion"] = 18;
            schemaSeventeen["PendingMutagenicContributions"] = new JsonArray();
        }

        if (root is JsonObject schemaEighteen && schemaEighteen["SchemaVersion"]?.GetValue<int>() == 18)
        {
            schemaEighteen["SchemaVersion"] = 19;
            schemaEighteen["RareLabs"] = new JsonArray();
        }

        if (root is JsonObject schemaNineteen && schemaNineteen["SchemaVersion"]?.GetValue<int>() == 19)
        {
            schemaNineteen["SchemaVersion"] = 20;
            schemaNineteen["PendingRefineryTransactions"] = new JsonArray();
        }

        if (root is JsonObject schemaTwenty && schemaTwenty["SchemaVersion"]?.GetValue<int>() == 20)
        {
            schemaTwenty["SchemaVersion"] = 21;
            schemaTwenty["PendingRareLabContributions"] = new JsonArray();
        }

        if (root is JsonObject schemaTwentyOne && schemaTwentyOne["SchemaVersion"]?.GetValue<int>() == 21)
        {
            schemaTwentyOne["SchemaVersion"] = 22;
        }

        if (root is JsonObject schemaTwentyTwo && schemaTwentyTwo["SchemaVersion"]?.GetValue<int>() == 22)
        {
            schemaTwentyTwo["SchemaVersion"] = 23;
            schemaTwentyTwo["PendingRareLabCatalystTransactions"] ??= new JsonArray();
        }

        if (root is JsonObject schemaTwentyThree && schemaTwentyThree["SchemaVersion"]?.GetValue<int>() == 23)
        {
            schemaTwentyThree["SchemaVersion"] = SlimeSaveData.CurrentSchemaVersion;
            schemaTwentyThree["SlimeDaycareQuest"] ??= new JsonObject();
        }

        if (root is JsonObject jsonData && jsonData["Slimes"] is JsonArray slimes)
        {
            foreach (JsonNode? slimeNode in slimes)
            {
                if (slimeNode is not JsonObject record || !record.TryGetPropertyValue("LikedFoodItemId", out JsonNode? oldLike))
                    continue;

                if (record["Genome"] is JsonObject genome && !genome.ContainsKey("LikedFoodItemId") && oldLike is not null)
                    genome["LikedFoodItemId"] = JsonNode.Parse(oldLike.ToJsonString());

                record.Remove("LikedFoodItemId");
            }
        }

        SlimeSaveData? data = root?.Deserialize<SlimeSaveData>()
            ?? throw new InvalidDataException($"Legacy slime registry '{this.registryPath}' is empty or invalid.");
        this.ImportedLegacyData = true;
        return data;
    }

    public void Save(SlimeSaveData data)
    {
        this.data.WriteSaveData(SaveDataKey, data);
        this.LoadedSchemaVersion = data.SchemaVersion;
        this.ImportedLegacyData = false;
    }

    public string? CreateExternalDataBackup()
    {
        string saveDirectory = GetSaveDirectory(this.modDirectory);
        if (!Directory.Exists(saveDirectory))
            return null;

        string saveName = Constants.SaveFolderName!;
        string backupDirectory = Path.Combine(this.modDirectory, "data", "backups");
        Directory.CreateDirectory(backupDirectory);
        string fileName = $"{saveName}-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.zip";
        string backupPath = Path.Combine(backupDirectory, fileName);
        ZipFile.CreateFromDirectory(saveDirectory, backupPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        return backupPath;
    }

    public static string GetSaveDirectory(string modDirectory)
    {
        string saveName = Constants.SaveFolderName
            ?? throw new InvalidOperationException("No save name is available for the external slime state.");
        if (saveName.Length == 0 || saveName is "." or ".." || Path.IsPathRooted(saveName)
            || Path.GetFileName(saveName) != saveName)
            throw new InvalidOperationException("The current save name is not a safe folder name.");

        return Path.Combine(modDirectory, "data", "saves", saveName);
    }

    private static string NormalizeEggToken(string value)
    {
        if (value.StartsWith(SlimeEggContent.ItemIdPrefix, StringComparison.Ordinal))
            value = value[SlimeEggContent.ItemIdPrefix.Length..];
        return Guid.TryParseExact(value, "N", out Guid id) ? id.ToString("N") : value;
    }
}
