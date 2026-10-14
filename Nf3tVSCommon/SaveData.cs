namespace Nf3t.VintageStory.Common;

public class SaveData
{
    public SaveData()
    {
        TranslocatorsPerSavegame = new Dictionary<string, Dictionary<SimplePos, SimplePos?>>();
        DefaultSpawnPositionPerSavegame = new Dictionary<string, SimplePos>();
        LastTranslocatorPathPerSavegame = new Dictionary<string, TranslocatorPath>();
        WayPointsPerSavegame = new Dictionary<string, HashSet<WayPoint>?>();
    }

    public Dictionary<string, Dictionary<SimplePos, SimplePos?>> TranslocatorsPerSavegame { get; }
    public Dictionary<string, SimplePos> DefaultSpawnPositionPerSavegame { get; }
    public Dictionary<string, TranslocatorPath> LastTranslocatorPathPerSavegame { get; }
    public Dictionary<string, HashSet<WayPoint>?> WayPointsPerSavegame { get; }

    public void Load(SerializedSaveData serializedSaveData)
    {
        foreach (var (savegameIdentifier, serializedEntries) in serializedSaveData.TranslocatorsPerSavegame)
        {
            TranslocatorsPerSavegame.TryGetValue(savegameIdentifier, out var entries);
            if (entries == null)
            {
                entries = new Dictionary<SimplePos, SimplePos?>();
                TranslocatorsPerSavegame[savegameIdentifier] = entries;
            }

            foreach (var entry in serializedEntries) entries[entry.Position] = entry.TargetLocation;
        }

        foreach (var (savegameIdentifier, defaultSpawnPosition) in serializedSaveData.DefaultSpawnPositionPerSavegame)
            DefaultSpawnPositionPerSavegame[savegameIdentifier] = defaultSpawnPosition;

        foreach (var (savegameIdentifier, path) in serializedSaveData.LastTranslocatorPathPerSavegame)
            LastTranslocatorPathPerSavegame[savegameIdentifier] = path;

        foreach (var (savegameIdentifier, waypoints) in serializedSaveData.WayPointsPerSavegame)
            WayPointsPerSavegame[savegameIdentifier] = new HashSet<WayPoint>(waypoints);
    }

    /// <summary>
    /// Merges what is currently on disk into this instance, so that saving does not discard changes
    /// another mod (sharing the same file) wrote since this one loaded it.
    /// Translocators are always unioned. The path and waypoints are only kept from memory when this
    /// instance changed them; otherwise the on-disk value is newer than ours.
    /// </summary>
    public void Merge(SerializedSaveData disk, bool keepMemoryPaths, bool keepMemoryWayPoints)
    {
        foreach (var (savegameIdentifier, serializedEntries) in disk.TranslocatorsPerSavegame)
        {
            if (!TranslocatorsPerSavegame.TryGetValue(savegameIdentifier, out var entries))
            {
                entries = new Dictionary<SimplePos, SimplePos?>();
                TranslocatorsPerSavegame[savegameIdentifier] = entries;
            }

            foreach (var entry in serializedEntries)
                // Never downgrade a known destination to "unknown".
                if (!entries.TryGetValue(entry.Position, out var existing) ||
                    (existing == null && entry.TargetLocation != null))
                    entries[entry.Position] = entry.TargetLocation;
        }

        foreach (var (savegameIdentifier, defaultSpawnPosition) in disk.DefaultSpawnPositionPerSavegame)
            DefaultSpawnPositionPerSavegame.TryAdd(savegameIdentifier, defaultSpawnPosition);

        foreach (var (savegameIdentifier, path) in disk.LastTranslocatorPathPerSavegame)
            if (!keepMemoryPaths || !LastTranslocatorPathPerSavegame.ContainsKey(savegameIdentifier))
                LastTranslocatorPathPerSavegame[savegameIdentifier] = path;

        foreach (var (savegameIdentifier, waypoints) in disk.WayPointsPerSavegame)
        {
            // Mutate in place: callers may hold a reference to the existing set.
            if (!WayPointsPerSavegame.TryGetValue(savegameIdentifier, out var existing) || existing == null)
            {
                WayPointsPerSavegame[savegameIdentifier] = new HashSet<WayPoint>(waypoints);
                continue;
            }

            if (!keepMemoryWayPoints) existing.Clear();
            existing.UnionWith(waypoints);
        }
    }

    public SerializedSaveData Save()
    {
        SerializedSaveData serializedSaveData = new();
        foreach (var (savegameIdentifier, entries) in TranslocatorsPerSavegame)
        {
            List<TranslocatorEntry> serializedEntries = new();
            serializedSaveData.TranslocatorsPerSavegame[savegameIdentifier] = serializedEntries;

            foreach (var (position, targetLocation) in entries)
                serializedEntries.Add(new TranslocatorEntry
                {
                    Position = position,
                    TargetLocation = targetLocation
                });
        }

        foreach (var (savegameIdentifier, defaultSpawnPosition) in DefaultSpawnPositionPerSavegame)
            serializedSaveData.DefaultSpawnPositionPerSavegame[savegameIdentifier] = defaultSpawnPosition;

        foreach (var (savegameIdentifier, path) in LastTranslocatorPathPerSavegame)
            serializedSaveData.LastTranslocatorPathPerSavegame[savegameIdentifier] = path;

        foreach (var (savegameIdentifier, wayPoints) in WayPointsPerSavegame)
            serializedSaveData.WayPointsPerSavegame[savegameIdentifier] =
                wayPoints == null ? new List<WayPoint>() : wayPoints.ToList();

        return serializedSaveData;
    }
}