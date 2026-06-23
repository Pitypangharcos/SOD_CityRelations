using System.Text.Json;
using SOD_CityRelations.Models;

namespace SOD_CityRelations.Services;

public sealed class RelationshipPersistenceService
{
    private const int CurrentSaveVersion = 1;
    private readonly string savePath;
    private readonly TimeSpan throttleInterval;
    private readonly ICityRelationsLogger logger;
    private DateTime lastSaveUtc = DateTime.MinValue;
    private bool pendingSave;

    public RelationshipPersistenceService(string baseDirectory, TimeSpan throttleInterval, ICityRelationsLogger logger)
    {
        this.throttleInterval = throttleInterval;
        this.logger = logger;
        var directory = Path.Combine(baseDirectory, "SOD_CityRelations");
        Directory.CreateDirectory(directory);
        savePath = Path.Combine(directory, "relationships.json");
    }

    public IReadOnlyDictionary<int, CitizenRelationshipProfile> Load()
    {
        if (!File.Exists(savePath))
        {
            logger.Info("Relationship save file missing; starting with an empty relationship store.");
            SaveNow(new Dictionary<int, CitizenRelationshipProfile>());
            return new Dictionary<int, CitizenRelationshipProfile>();
        }

        try
        {
            var json = File.ReadAllText(savePath);
            var root = JsonSerializer.Deserialize<RelationshipSaveRoot>(json, JsonOptions()) ?? new RelationshipSaveRoot();
            if (root.SaveVersion > CurrentSaveVersion)
            {
                logger.Warning($"Relationship save version {root.SaveVersion} is newer than supported version {CurrentSaveVersion}; attempting best-effort load.");
            }

            logger.Info($"Loaded {root.Citizens.Count} relationship profile(s).");
            return root.Citizens;
        }
        catch (Exception ex)
        {
            logger.Error("Failed to load relationship save; preserving corrupted file and starting empty.", ex);
            TryMoveCorruptedFile();
            return new Dictionary<int, CitizenRelationshipProfile>();
        }
    }

    public void QueueSave(IReadOnlyDictionary<int, CitizenRelationshipProfile> citizens)
    {
        pendingSave = true;
        FlushIfDue(citizens);
    }

    public void FlushIfDue(IReadOnlyDictionary<int, CitizenRelationshipProfile> citizens)
    {
        if (!pendingSave || DateTime.UtcNow - lastSaveUtc < throttleInterval)
        {
            return;
        }

        SaveNow(citizens);
    }

    public void SaveNow(IReadOnlyDictionary<int, CitizenRelationshipProfile> citizens)
    {
        try
        {
            var root = new RelationshipSaveRoot
            {
                SaveVersion = CurrentSaveVersion,
                Citizens = citizens.ToDictionary(pair => pair.Key, pair => pair.Value)
            };

            File.WriteAllText(savePath, JsonSerializer.Serialize(root, JsonOptions()));
            lastSaveUtc = DateTime.UtcNow;
            pendingSave = false;
            logger.Info($"Saved {citizens.Count} relationship profile(s) to {savePath}.");
        }
        catch (Exception ex)
        {
            logger.Error("Failed to save relationship data.", ex);
        }
    }

    private void TryMoveCorruptedFile()
    {
        try
        {
            var backup = savePath + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            if (File.Exists(savePath))
            {
                File.Move(savePath, backup);
            }
        }
        catch (Exception ex)
        {
            logger.Warning("Could not move corrupted relationship save aside: " + ex.Message);
        }
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private sealed class RelationshipSaveRoot
    {
        public int SaveVersion { get; set; } = CurrentSaveVersion;
        public Dictionary<int, CitizenRelationshipProfile> Citizens { get; set; } = new();
    }
}
