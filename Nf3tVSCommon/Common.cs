#nullable enable

using System.Collections;
using System.Collections.ObjectModel;
using System.Text;
using Dijkstra.NET.Graph;
using Dijkstra.NET.ShortestPath;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.Common;

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
    
    [JsonProperty] public Dictionary<string, List<WayPoint>> WayPointsPerSavegame { get; } = new();
}

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
        {
            WayPointsPerSavegame[savegameIdentifier] =  new HashSet<WayPoint>(waypoints);
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
            serializedSaveData.WayPointsPerSavegame[savegameIdentifier] = wayPoints == null ? new() : wayPoints.ToList();
        
        return serializedSaveData;
    }
}

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

    public TextCommandResult GetCollectionPerSaveCount<T>(string name, Dictionary<string, T?> collection) where T : IEnumerable
    {
        var worldId = ClientApi.World.SavegameIdentifier;
    
        // Log available keys for debugging
        string keys = string.Join(", ", collection.Keys);
        ClientApi.Logger.Debug("Current World: {0}. Available worlds: [{1}].", worldId, keys);

        // 1. Calculate Local Count (Safe check for missing key or null value)
        int localCount = 0;
        if (collection.TryGetValue(worldId, out var value) && value != null)
        {
            localCount = value.Cast<object>().Count();
        }

        // 2. Calculate Global Count
        // We cast to object because T is a generic IEnumerable
        var globalCount = collection.Values
            .Sum(x => x == null ? 0: x.Cast<object>().Count());

        return TextCommandResult.Success(
            $"Currently seen {name} in current world: {localCount}. Across all worlds: {globalCount}.");
    }
}


public class WayPoint
{
    [JsonConstructor]
    public WayPoint(string codePath, BlockPos pos, string name, string icon, string color)
    {
        CodePath = codePath;
        Pos = pos;
        Name = name;
        Icon = icon;
        Color = color;
        ExtraChat = "";
    }

    [JsonProperty] public string CodePath { get; }

    [JsonProperty] public BlockPos Pos { get; }
    [JsonProperty] public string Name { get; }

    // https://wiki.vintagestory.at/VTML
    [JsonProperty] public string Icon { get; }
    [JsonProperty] public string Color { get; }
    [JsonProperty] public string ExtraChat { get; set; }

    public double DistanceTo(BlockPos other)
    {
        return Math.Sqrt(Pos.DistanceSqTo(other.X, other.Y, other.Z));
    }

    public string ToWaypointString()
    {
        return $"/waypoint addati {Icon} ={Pos.X} ={Pos.Y} ={Pos.Z} false {Color} \"{Name}\"";
    }

    public string GetDirectionArrow(BlockPos playerPos)
    {
        // Calculate difference (Target - Player)
        double dz = Pos.Z - playerPos.Z;
        double dx = Pos.X - playerPos.X;

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

    public string ToRelativeCoordinates(BlockPos mapMiddlePos, BlockPos playerPos)
    {
        var distance = (int)DistanceTo(playerPos);
        var arrow = GetDirectionArrow(playerPos);
        var relativeX = Pos.X - mapMiddlePos.X;
        var relativeZ = Pos.Z - mapMiddlePos.Z;

        return $"{relativeX}, {Pos.Y}, {relativeZ} ({distance}m {arrow})";
    }

    public string ToChatString(BlockPos mapMiddlePos, BlockPos playerPos)
    {
        return $"<font color=\"{Color}\">[{Name}]</font> " +
               $"at <strong>{ToRelativeCoordinates(mapMiddlePos, playerPos)}{ExtraChat}</strong>";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not WayPoint other) return false;
        // We define a duplicate by its position. 
        // Usually, you don't want two waypoints at the exact same spot.
        return Pos.X == other.Pos.X && Pos.Y == other.Pos.Y && Pos.Z == other.Pos.Z;
    }

    public override int GetHashCode()
    {
        // Only hash the Position, as that is our "unique key"
        return HashCode.Combine(Pos.X, Pos.Y, Pos.Z);
    }
}

public class BlockSelector
{
    public BlockSelector()
    {
    }

    [JsonConstructor]
    public BlockSelector(string startsWith, string icon, string color, string[] keywords)
    {
        StartsWith = startsWith;
        Color = color;
        Icon = icon;
        Keywords = keywords;
    }

    [JsonProperty] public string StartsWith { get; set; } = "";
    [JsonProperty] public string Color { get; set; } = "";
    [JsonProperty] public string Icon { get; set; } = "";
    [JsonProperty] public string[] Keywords { get; set; } = new string[] { };
}

public class LocatorCommand
{
    public LocatorCommand()
    {
    }

    [JsonConstructor]
    public LocatorCommand(string name, string description, bool closestOnly, string keyword)
    {
        Name = name;
        Description = description;
        ClosestOnly = closestOnly;
        Keyword = keyword;
    }

    [JsonProperty] public string Name { get; set; } = "";
    [JsonProperty] public string Description { get; set; } = "";
    [JsonProperty] public bool ClosestOnly { get; set; } = true;
    [JsonProperty] public string Keyword { get; set; } = "";
}

public class Nf3tConfig
{
    public static string ConfigName = "Nf3tConfig.json";

    public int DefaultSearchRadius { get; set; } = 150;
    public List<BlockSelector> Selectors { get; set; } = new();
    public List<LocatorCommand> Commands { get; set; } = new();
    public bool EnableTranslocatorPath { get; set; }

    public string GenerateCommandsTable()
    {
        var sb = new StringBuilder();

        sb.AppendLine("<table>");
        sb.AppendLine("  <tr><th>Command</th><th>Description</th></tr>");
        sb.AppendLine("  <tr><td>.findtl</td><td>Finds nearby translocators.</td></tr>");

        foreach (var cmd in Commands) sb.AppendLine($"  <tr><td>.{cmd.Name}</td><td>{cmd.Description}</td></tr>");

        sb.AppendLine("</table>");
        return sb.ToString();
    }

    public static Nf3tConfig GetDefault()
    {
        return new Nf3tConfig
        {
            EnableTranslocatorPath = false,
            DefaultSearchRadius = 150,
            Selectors = new List<BlockSelector>
            {
                new("gear-rusty", "gear", "brown", ["treasure"]),
                new("tapestry", "vessel", "blue", ["art", "treasure"]),
                new("painting", "vessel", "blue", ["art", "treasure"]),
                new("chandelier", "vessel", "brown", ["art", "treasure"]),
                new("trunk", "vessel", "brown", ["chest", "treasure"]),
                new("lootvessel", "vessel", "brown", ["chest", "treasure"]),
                new("storagevessel", "vessel", "brown", ["chest", "treasure"]),
                new("talldisplaycase", "vessel", "brown", ["chest", "treasure"]),
                new("displaycase", "vessel", "brown", ["chest", "treasure"]),
                new("bonysoil", "rocks", "brown", ["soil"]),
                new("soil-high", "rocks", "brown", ["soil"]),
                new("soil-compost", "rocks", "brown", ["soil"]),
                new("rawclay-fire", "rocks", "orange", ["clay"]),
                new("rawclay-red", "rocks", "red", ["clay"]),
                new("rawclay-blue", "rocks", "blue", ["clay"]),
                new("rock-whitemarble", "rocks", "white", ["marble"]),
                new("rock-redmarble", "rocks", "red", ["marble"]),
                new("rock-greenmarble", "rocks", "green", ["marble"]),
                new("looseores", "pick", "yellow", ["ore"]),
                new("ore", "pick", "red", ["ore"]),
                new("crystal", "rocks", "black", ["ore"]),
                new("wildbeehives", "bee", "yellow", ["bees"]),
                new("skeep", "bee", "yellow", ["bees"]),
                new("log-resin", "tree", "yellow", ["resin"]),
                new("mushroom", "mushroom", "red", ["plant"]),
                new("fruittree", "tree2", "red", ["fruit"]),
                new("tallplant-tule", "x", "red", ["plant"]),
                new("tallplant-coopersreed", "x", "red", ["plant"]),
                new("tallplant-papyrus", "x", "red", ["plant"]),
                new("bigberrybush", "berries", "red", ["plant"]),
                new("smallberrybush", "berries", "red", ["plant"]),
                new("flower", "x", "red", ["plant"])
            },
            Commands = new List<LocatorCommand>
            {
                new("findtreasure", "Finds nearby treasure, and gears.", false, "treasure"),
                new("findart", "Finds nearby tapestries, paintings, and chandeliers.", false, "art"),
                new("findchest", "Finds nearby item containers.", false, "chest"),
                new("findsoil", "Finds nearby bony soil, and high fertility soil.", true, "soil"),
                new("findclay", "Finds nearby clay.", true, "clay"),
                new("findore", "Finds nearby ores, and crystals.", true, "ore"),
                new("findbees", "Finds nearby bees.", false, "bees"),
                new("findresin", "Finds nearby resin.", false, "resin"),
                new("findplants", "Finds nearby mushrooms, reeds, bushes, and flowers.", true, "plant"),
                new("findfruit", "Finds nearby fruit trees.", false, "fruit"),
                new("findmarble", "Finds nearby marble.", true, "marble"),
            }
        };
    }

    public static Nf3tConfig Load(ICoreClientAPI api)
    {
        Nf3tConfig? config = null;

        try
        {
            config = api.LoadModConfig<Nf3tConfig>(ConfigName);
        }
        catch (Exception e)
        {
            api.Logger.Error("Failed to load mod config, using defaults. Error: " + e.Message);
        }

        if (config == null || config.Commands == null || config.Selectors == null)
        {
            api.Logger.Notification("Config missing or invalid, generating default...");
            config = GetDefault();

            // Save the clean default so the user has a valid file to edit
            api.StoreModConfig(config, ConfigName);
        }

        return config;
    }
}
