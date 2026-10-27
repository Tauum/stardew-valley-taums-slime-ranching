using System.Text.Json;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;

namespace taums_slime_ranching.Services;

/// <summary>Creates a per-save baseline in the mod folder without writing to the game save.</summary>
internal static class FirstSeenSnapshot
{
    private const int SchemaVersion = 1;
    private const string SnapshotFileName = "first-seen-v1.json";

    public static string EnsureCreated(string modDirectory)
    {
        string savePath = Constants.CurrentSavePath
            ?? throw new InvalidOperationException("No save folder is available for the slime snapshot.");
        string saveFolderName = Constants.SaveFolderName
            ?? throw new InvalidOperationException("No save name is available for the slime snapshot.");
        string backupPath = Path.Combine(ModStateStore.GetSaveDirectory(modDirectory), "first-seen");
        string snapshotPath = Path.Combine(backupPath, SnapshotFileName);

        if (File.Exists(snapshotPath))
        {
            ValidateSnapshot(snapshotPath, backupPath, saveFolderName);
            return backupPath;
        }

        Directory.CreateDirectory(backupPath);

        // Read the original game save; all snapshot files are written under the mod folder.
        bool hasMainSave = CopySaveFileIfPresent(savePath, backupPath, saveFolderName);
        bool hasSaveGameInfo = CopySaveFileIfPresent(savePath, backupPath, "SaveGameInfo");

        var snapshot = new Snapshot
        {
            Version = SchemaVersion,
            SaveFolderName = saveFolderName,
            CapturedUtc = DateTime.UtcNow,
            HasMainSave = hasMainSave,
            HasSaveGameInfo = hasSaveGameInfo
        };

        Utility.ForEachLocation(location =>
        {
            if (location is SlimeHutch hutch)
            {
                foreach (GreenSlime slime in hutch.characters.OfType<GreenSlime>())
                {
                    var color = slime.color.Value;
                    snapshot.HutchSlimes.Add(new SnapshotSlime
                    {
                        HutchName = hutch.NameOrUniqueName,
                        Name = slime.Name,
                        PositionX = slime.Position.X,
                        PositionY = slime.Position.Y,
                        Color = $"#{color.R:X2}{color.G:X2}{color.B:X2}"
                    });
                }
            }

            return true;
        });

        if (snapshot.HutchSlimes.Count > 0 && !hasMainSave)
            throw new InvalidDataException("Legacy hutch slimes were found, but the original save file could not be backed up.");

        // Publish the marker only after the complete snapshot has been flushed.
        string pendingPath = snapshotPath + ".pending";
        using (var stream = new FileStream(pendingPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, snapshot, new JsonSerializerOptions { WriteIndented = true });
            stream.Flush(flushToDisk: true);
        }
        File.Move(pendingPath, snapshotPath);

        ValidateSnapshot(snapshotPath, backupPath, saveFolderName);
        return backupPath;
    }

    private static bool CopySaveFileIfPresent(string savePath, string backupPath, string fileName)
    {
        string source = Path.Combine(savePath, fileName);
        if (!File.Exists(source))
            return false;

        string destination = Path.Combine(backupPath, fileName);
        if (!File.Exists(destination))
        {
            string pendingPath = destination + ".pending";
            File.Copy(source, pendingPath, overwrite: true);
            if (new FileInfo(pendingPath).Length != new FileInfo(source).Length)
                throw new InvalidDataException($"The slime backup file '{pendingPath}' is incomplete.");
            File.Move(pendingPath, destination);
        }

        if (new FileInfo(destination).Length == 0)
            throw new InvalidDataException($"The slime backup file '{destination}' is empty.");

        return true;
    }

    private static void ValidateSnapshot(string snapshotPath, string backupPath, string saveFolderName)
    {
        Snapshot? snapshot;
        using (var stream = File.OpenRead(snapshotPath))
            snapshot = JsonSerializer.Deserialize<Snapshot>(stream);

        if (snapshot is null || snapshot.Version != SchemaVersion || snapshot.SaveFolderName != saveFolderName
            || snapshot.CapturedUtc == default || snapshot.HutchSlimes is null
            || (snapshot.HutchSlimes.Count > 0 && !snapshot.HasMainSave))
            throw new InvalidDataException($"The slime snapshot '{snapshotPath}' is invalid or belongs to another save.");

        if ((snapshot.HasMainSave && !IsNonEmptyFile(Path.Combine(backupPath, saveFolderName)))
            || (snapshot.HasSaveGameInfo && !IsNonEmptyFile(Path.Combine(backupPath, "SaveGameInfo"))))
            throw new InvalidDataException($"The slime snapshot '{snapshotPath}' is missing a save backup file.");
    }

    private static bool IsNonEmptyFile(string path) => File.Exists(path) && new FileInfo(path).Length > 0;

    private sealed class Snapshot
    {
        public Snapshot() { }

        public int Version { get; set; }
        public string SaveFolderName { get; set; } = "";
        public DateTime CapturedUtc { get; set; }
        public bool HasMainSave { get; set; }
        public bool HasSaveGameInfo { get; set; }
        public List<SnapshotSlime> HutchSlimes { get; set; } = new();
    }

    private sealed class SnapshotSlime
    {
        public SnapshotSlime() { }

        public string HutchName { get; set; } = "";
        public string Name { get; set; } = "";
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public string Color { get; set; } = "";
    }
}
