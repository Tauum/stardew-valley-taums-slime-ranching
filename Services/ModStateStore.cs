using System.Text.Json;
using System.Text.Json.Nodes;
using StardewModdingAPI;
using taums_slime_ranching.Models;

namespace taums_slime_ranching.Services;

/// <summary>Versioned per-save state located entirely under the installed mod folder.</summary>
internal sealed class ModStateStore
{
    private const string RegistryFileName = "slime-registry-v1.json";
    private readonly string modDirectory;
    private string? registryPath;
    private bool legacyFoodLikeMoved;
    private bool schemaMigrated;
    private string schemaBackupSuffix = ".before-managed-egg-incubation-v6.bak";

    public ModStateStore(string modDirectory) => this.modDirectory = modDirectory;

    public SlimeSaveData? Load()
    {
        this.legacyFoodLikeMoved = false;
        this.schemaMigrated = false;
        this.schemaBackupSuffix = ".before-managed-egg-incubation-v6.bak";
        this.registryPath = Path.Combine(GetSaveDirectory(this.modDirectory), RegistryFileName);
        if (!File.Exists(this.registryPath))
            return null;

        using var stream = File.OpenRead(this.registryPath);
        JsonNode? root = JsonNode.Parse(stream);
        if (root is JsonObject rootObject && rootObject["SchemaVersion"]?.GetValue<int>() == 1)
        {
            rootObject["SchemaVersion"] = 2;
            rootObject["BreedingPairs"] = new JsonArray();
            this.schemaMigrated = true;
        }

        if (root is JsonObject schemaTwo && schemaTwo["SchemaVersion"]?.GetValue<int>() == 2)
        {
            schemaTwo["SchemaVersion"] = 3;
            schemaTwo["Eggs"] = new JsonArray();
            this.schemaMigrated = true;
        }

        if (root is JsonObject schemaThree && schemaThree["SchemaVersion"]?.GetValue<int>() == 3)
        {
            schemaThree["SchemaVersion"] = 4;
            this.schemaBackupSuffix = ".before-breeding-output-v4.bak";
            this.schemaMigrated = true;
        }

        if (root is JsonObject schemaFour && schemaFour["SchemaVersion"]?.GetValue<int>() == 4)
        {
            schemaFour["SchemaVersion"] = 5;
            this.schemaBackupSuffix = ".before-breeding-cooldown-v5.bak";
            this.schemaMigrated = true;
        }

        if (root is JsonObject schemaFive && schemaFive["SchemaVersion"]?.GetValue<int>() == 5)
        {
            schemaFive["SchemaVersion"] = SlimeSaveData.CurrentSchemaVersion;
            this.schemaBackupSuffix = ".before-managed-egg-incubation-v6.bak";
            this.schemaMigrated = true;
        }

        if (root is JsonObject data && data["Slimes"] is JsonArray slimes)
        {
            foreach (JsonNode? slimeNode in slimes)
            {
                if (slimeNode is not JsonObject record || !record.TryGetPropertyValue("LikedFoodItemId", out JsonNode? oldLike))
                    continue;

                if (record["Genome"] is JsonObject genome && !genome.ContainsKey("LikedFoodItemId") && oldLike is not null)
                    genome["LikedFoodItemId"] = JsonNode.Parse(oldLike.ToJsonString());

                record.Remove("LikedFoodItemId");
                this.legacyFoodLikeMoved = true;
            }
        }

        return root?.Deserialize<SlimeSaveData>()
            ?? throw new InvalidDataException($"External slime registry '{this.registryPath}' is empty or invalid.");
    }

    public void Save(SlimeSaveData data)
    {
        string path = this.registryPath
            ?? throw new InvalidOperationException("The external slime registry has not been loaded for a save.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (this.legacyFoodLikeMoved && File.Exists(path))
        {
            string backupPath = path + ".before-food-like-move.bak";
            if (!File.Exists(backupPath))
                File.Copy(path, backupPath);
        }

        if (this.schemaMigrated && File.Exists(path))
        {
            string backupPath = path + this.schemaBackupSuffix;
            if (!File.Exists(backupPath))
                File.Copy(path, backupPath);
        }

        string pendingPath = path + ".pending";
        using (var stream = new FileStream(pendingPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, data, new JsonSerializerOptions { WriteIndented = true });
            stream.Flush(flushToDisk: true);
        }

        File.Move(pendingPath, path, overwrite: true);
        this.legacyFoodLikeMoved = false;
        this.schemaMigrated = false;
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
}
