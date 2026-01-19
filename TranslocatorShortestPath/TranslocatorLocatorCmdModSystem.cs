#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Dijkstra.NET.Graph;
using Dijkstra.NET.ShortestPath;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VSTutorial;

public readonly record struct SimplePos(int X, int Y, int Z)
{
    public double DistanceTo(SimplePos other)
    {
        return Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2) + Math.Pow(Z - other.Z, 2));
    }

    public string ToRelativeString(SimplePos defaultSpawnPosition)
    {
        return $"{X - defaultSpawnPosition.X}, {Y}, {Z - defaultSpawnPosition.Z}";
    }

    public string GetDirectionArrow(SimplePos playerPos)
    {
        // Calculate difference (Target - Player)
        double dz = Z - playerPos.Z;
        double dx = X - playerPos.X;

        // Atan2 returns the angle in radians
        // Math.Atan2(y, x) -> we use Z as Y for the 2D plane
        var radians = Math.Atan2(dz, dx);
        var degrees = radians * (180 / Math.PI);

        // Normalize to 0-360 for easier mapping
        // 0 is East, 90 is South, 180 is West, 270 is North
        var angle = (degrees + 360) % 360;

        if (angle >= 337.5 || angle < 22.5) return "→"; // East
        if (angle >= 22.5 && angle < 67.5) return "↘"; // South-East
        if (angle >= 67.5 && angle < 112.5) return "↓"; // South
        if (angle >= 112.5 && angle < 157.5) return "↙"; // South-West
        if (angle >= 157.5 && angle < 202.5) return "←"; // West
        if (angle >= 202.5 && angle < 247.5) return "↖"; // North-West
        if (angle >= 247.5 && angle < 292.5) return "↑"; // North
        if (angle >= 292.5 && angle < 337.5) return "↗"; // North-East

        return "•";
    }

    public string ToRelativeString(SimplePos defaultSpawnPosition, SimplePos playerPosition)
    {
        var distance = (int)DistanceTo(playerPosition);
        var arrow = GetDirectionArrow(playerPosition);

        return $"{ToRelativeString(defaultSpawnPosition)} ({distance}m {arrow})";
    }
}

public readonly record struct TranslocatorEntry(SimplePos Position, SimplePos? TargetLocation);

public class TranslocatorPathResult
{
    private readonly SimplePos _goalPos;
    private readonly Graph<SimplePos, string> _graph;
    private readonly ShortestPathResult _graphResult;
    private readonly Dictionary<SimplePos, uint> _posToId;
    private readonly SimplePos _startPos;

    public TranslocatorPathResult(Dictionary<SimplePos, SimplePos?> translocators, SimplePos startPos,
        SimplePos goalPos)
    {
        _startPos = startPos;
        _goalPos = goalPos;
        _graph = new Graph<SimplePos, string>();
        _posToId = new Dictionary<SimplePos, uint>();

        var startId = GetId(_startPos);
        var goalId = GetId(_goalPos);

        // 2. Add Edges: The "Big Walk" (Baseline)
        _graph.Connect(startId, goalId, (int)_startPos.DistanceTo(_goalPos), "Walk");

        // 3. Add Edges: Translocator Links
        foreach (var (src, target) in translocators)
            if (target.HasValue)
            {
                var srcId = GetId(src);
                var targetId = GetId(target.Value);

                // The ONLY way to get from src to target is the 0-cost jump
                _graph.Connect(srcId, targetId, 0, "Translocation");
                // Static translocators are 2-way, so add the return jump
                _graph.Connect(targetId, srcId, 0, "Translocation");

                // Distance from Start to the Entrance
                _graph.Connect(startId, srcId, (int)_startPos.DistanceTo(src), "Walk");
                _graph.Connect(startId, targetId, (int)_startPos.DistanceTo(target.Value), "Walk");

                // Distance from the Exit to the Goal
                _graph.Connect(srcId, goalId, (int)src.DistanceTo(_goalPos), "Walk");
                _graph.Connect(targetId, goalId, (int)target.Value.DistanceTo(_goalPos), "Walk");
            }

        // 4. Add Edges: Chaining (Portals near each other)
        var allTps = translocators.Where(kvp => kvp.Value.HasValue).ToList();
        foreach (var tp1 in allTps)
        {
            var exitPos = tp1.Value!.Value;

            // Find all potential next jumps, sort by distance, and take the closest 5-10
            var nearbyLinks = allTps
                .Where(tp2 => tp1.Key != tp2.Key)
                .Select(tp2 => new { Target = tp2.Key, Dist = exitPos.DistanceTo(tp2.Key) })
                .OrderBy(link => link.Dist)
                .Take(5); // Adjust this '5' based on performance needs

            foreach (var link in nearbyLinks)
            {
                var idA = GetId(exitPos);
                var idB = GetId(link.Target);

                // Connect both ways so the pathfinder can travel the "hallway" in either direction
                _graph.Connect(idA, idB, (int)link.Dist, "Walk");
                _graph.Connect(idB, idA, (int)link.Dist, "Walk");
            }
        }

        // 5. Calculate
        _graphResult = _graph.Dijkstra(startId, goalId);

        Path = _graphResult.GetPath().Select(id => _graph[id].Item).ToList().AsReadOnly();
    }

    public ReadOnlyCollection<SimplePos> Path { get; }

    private uint GetId(SimplePos p)
    {
        if (_posToId.TryGetValue(p, out var id)) return id;
        return _posToId[p] = _graph.AddNode(p);
    }

    public long GetBirdsEyeDistance()
    {
        return (long)_startPos.DistanceTo(_goalPos);
    }

    public long GetTotalDistance()
    {
        return _graphResult.Distance;
    }

    public bool IsFounded()
    {
        return _graphResult.IsFounded;
    }

    public SimplePos? GetNextStep()
    {
        if (Path.Count <= 1) return null;
        return Path[1];
    }
}

public readonly record struct TranslocatorPath(SimplePos StartPos, SimplePos GoalPos);

public class SerializedSaveData
{
    [JsonProperty] public Dictionary<string, List<TranslocatorEntry>> TranslocatorsPerSavegame { get; } = new();

    [JsonProperty] public Dictionary<string, SimplePos> DefaultSpawnPositionPerSavegame { get; } = new();

    [JsonProperty] public Dictionary<string, TranslocatorPath> LastTranslocatorPathPerSavegame { get; } = new();
}

public class SaveData
{
    public SaveData()
    {
        TranslocatorsPerSavegame = new Dictionary<string, Dictionary<SimplePos, SimplePos?>>();
        DefaultSpawnPositionPerSavegame = new Dictionary<string, SimplePos>();
        LastTranslocatorPathPerSavegame = new Dictionary<string, TranslocatorPath>();
    }

    public Dictionary<string, Dictionary<SimplePos, SimplePos?>> TranslocatorsPerSavegame { get; }
    public Dictionary<string, SimplePos> DefaultSpawnPositionPerSavegame { get; }
    public Dictionary<string, TranslocatorPath> LastTranslocatorPathPerSavegame { get; }

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

        return serializedSaveData;
    }
}

public class Context
{
    public Context(ICoreClientAPI clientApi)
    {
        ClientApi = clientApi;
        SaveData = new SaveData();
        SaveFilePath = Path.Combine(GamePaths.DataPath, "ModData", "found_translocators.json");
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
    public string SaveFilePath { get; }
    public bool IsDirty { get; set; }
    public SimplePos DefaultSpawnPosition { get; }
    public Dictionary<SimplePos, SimplePos?> Translocators { get; }
    public TranslocatorPathResult? TranslocatorPathResult { get; set; }

    public void Load()
    {
        Load(SaveFilePath);
    }

    public void Load(string path)
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

    public static SimplePos GetSimplePos(EntityPos pos)
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
}

public class TranslocatorLocatorCmdModSystem : ModSystem
{
    public Context Context { get; set; }

    public override bool ShouldLoad(EnumAppSide side)
    {
        return side == EnumAppSide.Client;
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        Context = new Context(api);

        Context.Load();

        api.Event.LeaveWorld += () => Context.Save();

        api.Event.RegisterGameTickListener(_ => Context.Save(), 5000);
        api.Event.RegisterGameTickListener(_ =>
        {
            if (!api.Input.MouseGrabbed || api.World.Player.Entity.State != EnumEntityState.Active) return;

            var selection = api.World.Player.CurrentBlockSelection;
            if (selection == null) return;

            if (api.World.BlockAccessor.GetBlockEntity(selection.Position) is BlockEntityStaticTranslocator
                translocator)
            {
                var source = new SimplePos
                    { X = selection.Position.X, Y = selection.Position.Y, Z = selection.Position.Z };

                if (translocator.TargetLocation != null)
                {
                    var target = new SimplePos
                    {
                        X = translocator.TargetLocation.X, Y = translocator.TargetLocation.Y,
                        Z = translocator.TargetLocation.Z
                    };
                    Context.AddTranslocator(source, target);
                    Context.AddTranslocator(target, source);
                }
                else
                {
                    Context.AddTranslocator(source, null);
                }
            }
        }, 200);
        
        api.ChatCommands.Create("pathtl")
            .WithDescription("Find shortest path to coordinates using known translocators, by specifying an optional target location, and start location. Or will fall back to previous target location, and current player position.")
            .WithArgs(api.ChatCommands.Parsers.OptionalWorldPosition("goal"), api.ChatCommands.Parsers.OptionalWorldPosition("start"))
            .HandleWith(args =>
            {
                var playerPos = Context.GetPlayerPos();
                
                var goalArg = Context.GetSimplePos((Vec3d)args[0]);
                var startArg = Context.GetSimplePos((Vec3d)args[1]);
                
                if (goalArg == startArg)
                {
                    if (Context.SaveData.LastTranslocatorPathPerSavegame.TryGetValue(
                            Context.ClientApi.World.SavegameIdentifier, out var path))
                    {
                        return CreateHandle(Context, playerPos, playerPos, path.GoalPos);
                    }

                    return TextCommandResult.Error("Did not find existing history, please provide at least one argument.");
                }
                
                return CreateHandle(Context, playerPos, startArg, goalArg);
            });
        
        api.ChatCommands.Create("pathtlhist")
            .WithDescription("Find shortest path to coordinates using known translocators with the previously given value.")
            .WithArgs()
            .HandleWith(_ =>
            {
                var playerPos = Context.GetPlayerPos();
                
                if (Context.SaveData.LastTranslocatorPathPerSavegame.TryGetValue(
                        Context.ClientApi.World.SavegameIdentifier, out var path))
                {
                    return CreateHandle(Context, playerPos, path.StartPos, path.GoalPos);
                }

                return TextCommandResult.Error("Did not find existing history.");
            });
        
        api.ChatCommands.Create("counttl")
            .WithDescription("Give a count of currently seen translocators.")
            .WithArgs()
            .HandleWith(_ => TextCommandResult.Success($"Currently seen translocators in current world: {Context.Translocators.Count}."));
    }

    private static TextCommandResult CreateHandle(Context context, SimplePos playerPos, SimplePos startPos, SimplePos goalPos)
    {
        var result = context.CalculatePath(playerPos, goalPos);

        var birdsEyeDistance = result.GetBirdsEyeDistance();

        if (result.IsFounded())
        {
            var steps = string.Join(" \u21D2 ",
                result.Path.Select(p => p.ToRelativeString(context.DefaultSpawnPosition)));

            return TextCommandResult.Success(
                $"<strong>Next:</strong> {result.GetNextStep()?.ToRelativeString(context.DefaultSpawnPosition, playerPos)}\n<strong>Path distance:</strong> {result.GetTotalDistance()} block(s).\n<strong>Birds eye distance:</strong> {result.GetBirdsEyeDistance()} block(s).\n<strong>Path Count:</strong> {result.Path.Count}\n<strong>Full Path:</strong> {steps}");
        }

        return TextCommandResult.Success(
            $"No translocator shortcut found. Birds eye distance {birdsEyeDistance}");
    }
}