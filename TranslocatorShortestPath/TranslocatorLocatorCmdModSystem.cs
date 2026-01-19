#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dijkstra.NET.Graph;
using Dijkstra.NET.ShortestPath;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
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

public class TranslocatorLocatorCmdModSystem : ModSystem
{
    public override bool ShouldLoad(EnumAppSide side)
    {
        return side == EnumAppSide.Client;
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        Dictionary<string, Dictionary<SimplePos, SimplePos?>> coordinatesPerSavegame = new();
        Dictionary<SimplePos, SimplePos?> translocators = new();
        coordinatesPerSavegame[api.World.SavegameIdentifier] = translocators;
        var isDirty = false;
        var saveFilePath = Path.Combine(GamePaths.DataPath, "ModData", "found_translocators.json");

        var defaultSpawnPosition = new SimplePos
        {
            X = api.World.DefaultSpawnPosition.AsBlockPos.X,
            Y = api.World.DefaultSpawnPosition.AsBlockPos.Y,
            Z = api.World.DefaultSpawnPosition.AsBlockPos.Z
        };

        try
        {
            if (File.Exists(saveFilePath))
            {
                var json = File.ReadAllText(saveFilePath);

                var serializedWorldLut =
                    JsonConvert.DeserializeObject<Dictionary<string, List<TranslocatorEntry>>>(json);
                if (serializedWorldLut != null)
                    foreach (var (savegameIdentifier, serializedEntries) in serializedWorldLut)
                    {
                        coordinatesPerSavegame.TryGetValue(savegameIdentifier, out var entries);
                        if (entries == null)
                        {
                            entries = new Dictionary<SimplePos, SimplePos?>();
                            coordinatesPerSavegame[savegameIdentifier] = entries;
                        }

                        foreach (var entry in serializedEntries) entries[entry.Position] = entry.TargetLocation;
                    }
            }
        }
        catch (Exception e)
        {
            api.Logger.Error(e);
        }

        Action<SimplePos, SimplePos?> addPos = (key, value) =>
        {
            if (!translocators.TryGetValue(key, out var existing) || existing != value)
            {
                translocators[key] = value;
                isDirty = true; // Mark for saving later
                api.Logger.Debug("[Translocator Locator] Recorded: {0} -> {1}", key, value?.ToString() ?? "Unknown");
            }
        };

        var save = () =>
        {
            if (isDirty)
                try
                {
                    var directoryPath = Path.GetDirectoryName(saveFilePath);
                    if (directoryPath != null) Directory.CreateDirectory(directoryPath);

                    Dictionary<string, List<TranslocatorEntry>> serializedWorldLut = new();
                    foreach (var (savegameIdentifier, entries) in coordinatesPerSavegame)
                    {
                        List<TranslocatorEntry> serializedEntries = new();
                        serializedWorldLut[savegameIdentifier] = serializedEntries;

                        foreach (var (position, targetLocation) in entries)
                            serializedEntries.Add(new TranslocatorEntry
                            {
                                Position = position,
                                TargetLocation = targetLocation
                            });
                    }

                    var json = JsonConvert.SerializeObject(serializedWorldLut, Formatting.Indented);
                    File.WriteAllText(saveFilePath, json);
                    isDirty = false;
                    int totalWaypoints = coordinatesPerSavegame.Values.Sum(dict => dict.Count);
                    api.Logger.Debug($"[Translocator Locator] {totalWaypoints} Waypoint(s) saved to disk.");
                }
                catch (Exception e)
                {
                    api.Logger.Error(e);
                }
        };

        api.Event.LeaveWorld += () => save();

        api.Event.RegisterGameTickListener(_ => save(), 5000);
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
                    addPos(source, target);
                    addPos(target, source);
                }
                else
                {
                    addPos(source, null);
                }
            }
        }, 200);

        // TODO: -28719 1 -7630
        
        api.ChatCommands.Create("pathtl")
            .WithDescription("Find shortest path to coordinates using known translocators")
            .WithArgs(api.ChatCommands.Parsers.Int("x"), api.ChatCommands.Parsers.Int("y"),
                api.ChatCommands.Parsers.Int("z"))
            .HandleWith(args =>
            {
                var start = new SimplePos((int)api.World.Player.Entity.Pos.X, (int)api.World.Player.Entity.Pos.Y,
                    (int)api.World.Player.Entity.Pos.Z);
                var goal = new SimplePos(defaultSpawnPosition.X + (int)args[0], (int)args[1],
                    defaultSpawnPosition.Z + (int)args[2]);

                // 1. Setup Graph and ID Mapping
                var graph = new Graph<SimplePos, string>();
                var posToId = new Dictionary<SimplePos, uint>();

                uint GetId(SimplePos p)
                {
                    if (posToId.TryGetValue(p, out var id)) return id;
                    return posToId[p] = graph.AddNode(p);
                }

                var startId = GetId(start);
                var goalId = GetId(goal);

                // 2. Add Edges: The "Big Walk" (Baseline)
                graph.Connect(startId, goalId, (int)start.DistanceTo(goal), "Walk");

                // 3. Add Edges: Translocator Links
                foreach (var (src, target) in translocators)
                    if (target.HasValue)
                    {
                        uint srcId = GetId(src);
                        uint targetId = GetId(target.Value);

                        // The ONLY way to get from src to target is the 0-cost jump
                        graph.Connect(srcId, targetId, 0, "Translocation");
                        // Static translocators are 2-way, so add the return jump
                        graph.Connect(targetId, srcId, 0, "Translocation");

                        // Distance from Start to the Entrance
                        graph.Connect(startId, srcId, (int)start.DistanceTo(src), "Walk");
                        graph.Connect(startId, targetId, (int)start.DistanceTo(target.Value), "Walk");

                        // Distance from the Exit to the Goal
                        graph.Connect(srcId, goalId, (int)src.DistanceTo(goal), "Walk");
                        graph.Connect(targetId, goalId, (int)target.Value.DistanceTo(goal), "Walk");
                    }

                // 4. Add Edges: Chaining (Portals near each other)
                var allTps = translocators.Where(kvp => kvp.Value.HasValue).ToList();
                foreach (var tp1 in allTps)
                foreach (var tp2 in allTps)
                {
                    if (tp1.Key == tp2.Key) continue;
                    var distBetween = tp1.Value!.Value.DistanceTo(tp2.Key);

                    if (distBetween < 10_000) 
                    {
                        graph.Connect(GetId(tp1.Value.Value), GetId(tp2.Key), (int)distBetween, "Walk");
                    }
                }

                // 5. Calculate
                var result = graph.Dijkstra(startId, goalId);

                var birdsEyeDistance = (long) start.DistanceTo(goal);
                
                if (result.IsFounded)
                {
                    var pathNodes = result.GetPath().ToList();
                    var pathPositions = pathNodes.Select(id => graph[id].Item).ToList();

                    var totalDist = (long) result.Distance;
                    
                    var steps = string.Join(" \u21D2 ", pathPositions.Select(p => p.ToRelativeString(defaultSpawnPosition)));

                    return TextCommandResult.Success($"<strong>Next:</strong> {pathPositions[1].ToRelativeString(defaultSpawnPosition, start)}\n<strong>Path distance:</strong> {totalDist} block(s).\n<strong>Birds eye distance:</strong> {birdsEyeDistance} block(s).\n<strong>Path Count:</strong> {pathPositions.Count}\n<strong>Full Path:</strong> {steps}");
                }
                return TextCommandResult.Success($"No translocator shortcut found. Birds eye distance {birdsEyeDistance}");
            });
    }
}