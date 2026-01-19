#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace VSTutorial;

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

    public void CreateCommand(ICoreClientAPI api, string saveFilePath, TranslocatorLocatorConfig config)
    {
        System.Func<BlockSelector, bool> predicate = s => s.Keywords.Contains(Keyword);

        api.ChatCommands.Create(Name)
            .WithDescription(Description)
            // Added a boolean parser. Optional(false) makes it default to false if omitted.
            .WithArgs(
                api.ChatCommands.Parsers.OptionalBool("addWaypoints"),
                api.ChatCommands.Parsers.OptionalInt("radius", config.DefaultSearchRadius))
            .HandleWith(args =>
            {
                var addWaypoints = true.Equals(args[0]);
                var radius = (int)args[1];

                var filteredSelectors = config.Selectors
                    .Where(predicate)
                    .ToList();

                Func<BlockPos, BlockPos, List<WayPoint>, Block, BlockPos, bool> onBlock =
                    (mapMiddlePos, playerPos, results, block, pos) =>
                    {
                        foreach (var selector in filteredSelectors)
                            if (block.Code.Path.StartsWith(selector.StartsWith))
                            {
                                var blockName = block.GetPlacedBlockName(api.World, pos);
                                var wayPoint = new WayPoint(block.Code.Path, pos.Copy(), blockName, selector.Icon,
                                    selector.Color);
                                results.Add(wayPoint);
                                return true;
                            }

                        return true;
                    };

                return TranslocatorLocatorCmdModSystem.ProcessFindBlock(api, saveFilePath, addWaypoints, radius,
                    ClosestOnly, onBlock);
            });
    }
}

public class TranslocatorLocatorConfig
{
    public static string ConfigName = "translocatorLocator.json";

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

    public static TranslocatorLocatorConfig GetDefault()
    {
        return new TranslocatorLocatorConfig
        {
            EnableTranslocatorPath = false,
            DefaultSearchRadius = 150,
            Selectors = new List<BlockSelector>
            {
                // TODO: Add gear pile
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
                new("findtreasure", "Finds nearby treasure.", false, "treasure"),
                new("findart", "Finds nearby tapestries, paintings, and chandeliers.", false, "art"),
                new("findchest", "Finds nearby item containers.", false, "chest"),
                new("findsoil", "Finds nearby bony soil, and high fertility soil.", true, "soil"),
                new("findclay", "Finds nearby clay.", true, "clay"),
                new("findore", "Finds nearby ores and crystals.", true, "ore"),
                new("findbees", "Finds nearby bees.", false, "bees"),
                new("findresin", "Finds nearby resin.", false, "resin"),
                new("findplants", "Finds nearby mushrooms, reeds, bushes.", true, "plant"),
                new("findfruit", "Finds nearby fruit trees.", false, "fruit")
            }
        };
    }

    public static TranslocatorLocatorConfig Load(ICoreClientAPI api)
    {
        TranslocatorLocatorConfig? config = null;

        try
        {
            config = api.LoadModConfig<TranslocatorLocatorConfig>(ConfigName);
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

public class TranslocatorLocatorCmdModSystem : ModSystem
{
    public override bool ShouldLoad(EnumAppSide side)
    {
        return side == EnumAppSide.Client;
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        var config = TranslocatorLocatorConfig.Load(api);

        var saveFilePath = Path.Combine(GamePaths.DataPath, "ModData", "found_waypoints.json");

        api.ChatCommands.Create("findtl")
            .WithDescription("Finds nearby translocators.")
            // Added a boolean parser. Optional(false) makes it default to false if omitted.
            .WithArgs(
                api.ChatCommands.Parsers.OptionalBool("addWaypoints"),
                api.ChatCommands.Parsers.OptionalInt("radius", config.DefaultSearchRadius))
            .HandleWith(args =>
            {
                var addWaypoints = true.Equals(args[0]);
                var radius = (int)args[1];

                return ProcessFindTranslocator(api, saveFilePath, addWaypoints, radius);
            });

        foreach (var command in config.Commands) command.CreateCommand(api, saveFilePath, config);
    }

    private static TextCommandResult ProcessFindTranslocator(ICoreClientAPI api, string saveFilePath, bool addWaypoints,
        int radius)
    {
        Func<BlockPos, BlockPos, List<WayPoint>, Block, BlockPos, bool> onBlock =
            (mapMiddlePos, playerPos, results, block, pos) =>
            {
                if (!block.Code.Path.StartsWith("statictranslocator")) return true;

                var isRepaired = false;

                if (block.Code.Path.StartsWith("statictranslocator-broken"))
                    isRepaired = false;
                else if (block.Code.Path.StartsWith("statictranslocator-normal")) isRepaired = true;

                BlockPos destination = null;

                if (block is BlockStaticTranslocator translocatorBlock)
                {
                    isRepaired = translocatorBlock.Repaired;
                    api.Logger.Debug("[Translocator Locator] Found repaired status: {0} for {1}",
                        translocatorBlock.Repaired, pos);
                    var be = api.World.BlockAccessor.GetBlockEntity(pos);
                    if (be is BlockEntityStaticTranslocator translocator)
                    {
                        destination = translocator.TargetLocation;
                        api.Logger.Debug("[Translocator Locator] Found destination: {0} for {1}", destination, pos);
                    }
                }

                api.Logger.Debug("[Translocator Locator] Found translocator block: {0} at {1}", block.Code.Path, pos);

                var sourceWayPoint = new WayPoint(block.Code.Path, pos.Copy(), block.GetPlacedBlockName(api.World, pos),
                    "spiral", isRepaired ? "green" : "red");
                results.Add(sourceWayPoint);

                if (destination != null)
                {
                    // TODO: Technically this codepath is incorrect as it is the source's path
                    var destinationWayPoint = new WayPoint(block.Code.Path, destination.Copy(),
                        block.GetPlacedBlockName(api.World, destination),
                        "spiral", "green");
                    results.Add(destinationWayPoint);
                    // TODO: Maybe just add destination to each waypoint...
                    destinationWayPoint.ExtraChat =
                        " to " + sourceWayPoint.ToRelativeCoordinates(mapMiddlePos, playerPos);
                    sourceWayPoint.ExtraChat =
                        " to " + destinationWayPoint.ToRelativeCoordinates(mapMiddlePos, playerPos);
                }

                return true;
            };

        return ProcessFindBlock(api, saveFilePath, addWaypoints, radius, false, onBlock);
    }

    public static TextCommandResult ProcessFindBlock(ICoreClientAPI api, string saveFilePath, bool addWaypoints,
        int radius, bool closestOnly, Func<BlockPos, BlockPos, List<WayPoint>, Block, BlockPos, bool> onBlock)
    {
        if (api.World?.Player?.Entity == null) return TextCommandResult.Error("Player not found.");

        var playerPos = api.World.Player.Entity.Pos.AsBlockPos;
        var mapMiddlePos = api.World.DefaultSpawnPosition.AsBlockPos;
        var results = new List<WayPoint>();
        Dictionary<string, HashSet<WayPoint>?> previousWayPoints = new();

        try
        {
            if (File.Exists(saveFilePath))
            {
                var json = File.ReadAllText(saveFilePath);
                var loaded = JsonConvert.DeserializeObject<Dictionary<string, HashSet<WayPoint>?>>(json);
                if (loaded != null) previousWayPoints = loaded;
            }
        }
        catch (Exception e)
        {
            api.Logger.Error(e);
        }

        // TODO: possibly make the Y always 0 - MAX HEIGHT?
        api.World.BlockAccessor.SearchBlocks(playerPos.AddCopy(-radius, -radius, -radius),
            playerPos.AddCopy(radius, radius, radius), (block, pos) =>
                onBlock(mapMiddlePos, playerPos, results, block, pos));

        IEnumerable<WayPoint> query = results
            .OrderBy(tl => tl.DistanceTo(playerPos));

        if (closestOnly)
            query = query.GroupBy(wp => wp.Name)
                .Select(group => group.First());

        var sortedResults = query.Reverse().ToList();

        var checkWayPoints = previousWayPoints.Get(api.World.SavegameIdentifier);
        if (checkWayPoints == null)
        {
            checkWayPoints = new HashSet<WayPoint>();
            previousWayPoints[api.World.SavegameIdentifier] = checkWayPoints;
        }

        var messages = new List<string>();
        foreach (var result in sortedResults)
        {
            messages.Add(result.ToChatString(mapMiddlePos, playerPos));
            if (addWaypoints)
            {
                if (checkWayPoints.Contains(result))
                    api.Logger.Debug($"WayPoint already exists: {result.Pos}");
                else
                    api.SendChatMessage(result.ToWaypointString());
            }
        }

        if (messages.Count > 0)
        {
            var fullMessage = string.Join("\n", messages);
            api.ShowChatMessage(fullMessage);

            if (addWaypoints)
                try
                {
                    var folderPath = Path.GetDirectoryName(saveFilePath);

                    // 2. Create the directory if it doesn't exist
                    if (folderPath != null && !Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                    checkWayPoints.UnionWith(sortedResults);

                    var json = JsonConvert.SerializeObject(previousWayPoints, Formatting.Indented);
                    File.WriteAllText(saveFilePath, json);
                    api.Logger.Debug($"Saved {checkWayPoints.Count} waypoints to {saveFilePath}");
                    return TextCommandResult.Success($"Found {sortedResults.Count} waypoints within {radius} blocks.");
                }
                catch (Exception e)
                {
                    return TextCommandResult.Error("Failed to save: " + e.Message);
                }

            return TextCommandResult.Success($"Found {sortedResults.Count} waypoints within {radius} blocks.");
        }

        return TextCommandResult.Success($"Not found within {radius} blocks.");
    }
}