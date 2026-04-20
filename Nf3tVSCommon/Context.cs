using System.Collections;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.Common;

public class Context
{
    public Context(ICoreClientAPI clientApi)
    {
        ClientApi = clientApi;
        SaveData = new SaveData();
        SaveFilePath = Path.Combine(GamePaths.DataPath, "ModData", "Nf3tData.json");
        IsDirty = false;
        DefaultSpawnPosition = new SimplePos(ClientApi.World.DefaultSpawnPosition.AsBlockPos.X,
            ClientApi.World.DefaultSpawnPosition.AsBlockPos.Y, ClientApi.World.DefaultSpawnPosition.AsBlockPos.Z);
        SaveData.DefaultSpawnPositionPerSavegame[ClientApi.World.SavegameIdentifier] = DefaultSpawnPosition;
        SaveData.TranslocatorsPerSavegame.TryGetValue(ClientApi.World.SavegameIdentifier, out var translocators);
        if (translocators == null)
        {
            translocators = new Dictionary<SimplePos, SimplePos?>();
            SaveData.TranslocatorsPerSavegame[ClientApi.World.SavegameIdentifier] = translocators;
        }

        Translocators = translocators;
    }

    public ICoreClientAPI ClientApi { get; }
    public SaveData SaveData { get; }
    private string SaveFilePath { get; }
    public bool IsDirty { get; set; }
    public SimplePos DefaultSpawnPosition { get; }
    private Dictionary<SimplePos, SimplePos?> Translocators { get; }
    private TranslocatorPathResult? TranslocatorPathResult { get; set; }

    public void Load()
    {
        Load(SaveFilePath);
    }

    private void Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return;
            var json = File.ReadAllText(path);
            var serializedSaveData = JsonConvert.DeserializeObject<SerializedSaveData>(json);
            if (serializedSaveData != null) SaveData.Load(serializedSaveData);
        }
        catch (Exception e)
        {
            ClientApi.Logger.Error(e);
        }
    }

    public void Save()
    {
        if (!IsDirty) return;

        try
        {
            var directoryPath = Path.GetDirectoryName(SaveFilePath);
            if (directoryPath != null) Directory.CreateDirectory(directoryPath);
            var serializedSaveData = SaveData.Save();

            var json = JsonConvert.SerializeObject(serializedSaveData, Formatting.Indented);
            File.WriteAllText(SaveFilePath, json);
            IsDirty = false;
            var totalWaypoints = serializedSaveData.TranslocatorsPerSavegame.Values.Sum(dict => dict.Count);
            ClientApi.Logger.Debug($"[Translocator Locator] {totalWaypoints} Waypoint(s) saved to disk.");
        }
        catch (Exception e)
        {
            ClientApi.Logger.Error(e);
        }
    }

    public void AddTranslocator(SimplePos position, SimplePos? targetLocation)
    {
        if (!Translocators.TryGetValue(position, out var existing) || existing != targetLocation)
        {
            Translocators[position] = targetLocation;
            IsDirty = true; // Mark for saving later
            ClientApi.Logger.Debug("[Translocator Locator] Recorded: {0} -> {1}", position,
                existing?.ToString() ?? "Unknown");
        }
    }

    public TranslocatorPathResult CalculatePath(SimplePos startPos,
        SimplePos goalPos)
    {
        SaveData.LastTranslocatorPathPerSavegame.TryGetValue(ClientApi.World.SavegameIdentifier, out var existing);
        if (TranslocatorPathResult != null && existing.StartPos == startPos && existing.GoalPos == goalPos)
            return TranslocatorPathResult;
        var result = new TranslocatorPathResult(Translocators, startPos, goalPos);
        SaveData.LastTranslocatorPathPerSavegame[ClientApi.World.SavegameIdentifier] =
            new TranslocatorPath(startPos, goalPos);
        IsDirty = true;
        TranslocatorPathResult = result;
        return result;
    }

    private static SimplePos GetSimplePos(EntityPos pos)
    {
        return new SimplePos((int)pos.X, (int)pos.Y,
            (int)pos.Z);
    }

    public static SimplePos GetSimplePos(Vec3d pos)
    {
        return new SimplePos((int)pos.X, (int)pos.Y,
            (int)pos.Z);
    }

    public SimplePos GetPlayerPos()
    {
        return GetSimplePos(ClientApi.World.Player.Entity.Pos);
    }

    public TextCommandResult GetCollectionPerSaveCount<T>(string name, Dictionary<string, T> collection)
        where T : IEnumerable?
    {
        var worldId = ClientApi.World.SavegameIdentifier;

        // Log available keys for debugging
        var keys = string.Join(", ", collection.Keys);
        ClientApi.Logger.Debug("Current World: {0}. Available worlds: [{1}].", worldId, keys);

        // 1. Calculate Local Count (Safe check for missing key or null value)
        var localCount = 0;
        if (collection.TryGetValue(worldId, out var value) && value != null) localCount = value.Cast<object>().Count();

        // 2. Calculate Global Count
        // We cast to object because T is a generic IEnumerable
        var globalCount = collection.Values
            .Sum(x => x?.Cast<object>().Count() ?? 0);

        return TextCommandResult.Success(
            $"Currently seen {name} in current world: {localCount}. Across all worlds: {globalCount}.");
    }
}