#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace VSTutorial
{
    public class WayPoint
    {
        [JsonProperty] public string CodePath { get; }

        [JsonProperty] public BlockPos Pos { get; }
        [JsonProperty] public string Name { get; }
        
        // https://wiki.vintagestory.at/VTML
        [JsonProperty] public string Icon { get; }
        [JsonProperty] public string Color { get; }
        [JsonProperty] public string ExtraChat { get; set; }

        [JsonConstructor] public WayPoint(string codePath, BlockPos pos, string name, string icon, string color)
        {
            CodePath = codePath;
            Pos = pos;
            Name = name;
            Icon = icon;
            Color = color;
            ExtraChat = "";
        }

        public double DistanceTo(BlockPos other)
        {
            return Math.Sqrt(Pos.DistanceSqTo(other.X, other.Y, other.Z));
        }
        
        public string ToWaypointString()
        {
            return $"/waypoint addati {Icon} ={(int)Pos.X} ={(int)Pos.Y} ={(int)Pos.Z} false {Color} \"{Name}\"";
        }
        
        public string GetDirectionArrow(BlockPos playerPos)
        {
            // Calculate difference (Target - Player)
            double dz = Pos.Z - playerPos.Z;
            double dx = Pos.X - playerPos.X;

            // Atan2 returns the angle in radians
            // Math.Atan2(y, x) -> we use Z as Y for the 2D plane
            double radians = Math.Atan2(dz, dx);
            double degrees = radians * (180 / Math.PI);

            // Normalize to 0-360 for easier mapping
            // 0 is East, 90 is South, 180 is West, 270 is North
            double angle = (degrees + 360) % 360;

            if (angle >= 337.5 || angle < 22.5)   return "→"; // East
            if (angle >= 22.5  && angle < 67.5)   return "↘"; // South-East
            if (angle >= 67.5  && angle < 112.5)  return "↓"; // South
            if (angle >= 112.5 && angle < 157.5)  return "↙"; // South-West
            if (angle >= 157.5 && angle < 202.5)  return "←"; // West
            if (angle >= 202.5 && angle < 247.5)  return "↖"; // North-West
            if (angle >= 247.5 && angle < 292.5)  return "↑"; // North
            if (angle >= 292.5 && angle < 337.5)  return "↗"; // North-East

            return "•"; 
        }
        
        public string ToRelativeCoordinates(BlockPos mapMiddlePos, BlockPos playerPos)
        {
            int distance = (int) DistanceTo(playerPos);
            string arrow = GetDirectionArrow(playerPos);
            int relativeX = Pos.X - mapMiddlePos.X;
            int relativeZ = Pos.Z - mapMiddlePos.Z;
            
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
    
    public class BlockSelector {
        [JsonProperty] public string StartsWith { get; set; } = "";
        [JsonProperty] public string Color { get; set; } = "";
        [JsonProperty] public string Icon { get; set; } = "";
        [JsonProperty] public string[] Keywords { get; set; } = new string[] { };

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
    }

    public class LocatorCommand
    {
        [JsonProperty] public string Name { get; set; } =  "";
        [JsonProperty] public string Description { get;set; } =  "";
        [JsonProperty] public bool ClosestOnly { get; set;} = true;
        [JsonProperty] public string Keyword { get; set;} =  "";

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

        public void CreateCommand(ICoreClientAPI api, String saveFilePath, TranslocatorLocatorConfig config)
        {
            
            System.Func<BlockSelector,bool> predicate = s => s.Keywords.Contains(this.Keyword);
        
            api.ChatCommands.Create(this.Name)
                .WithDescription(this.Description)
                // Added a boolean parser. Optional(false) makes it default to false if omitted.
                .WithArgs(
                    api.ChatCommands.Parsers.OptionalBool("addWaypoints"),
                    api.ChatCommands.Parsers.OptionalInt("radius", config.DefaultSearchRadius))
                .HandleWith(args => 
                {
                    bool addWaypoints = true.Equals(args[0]);
                    int radius =(int)args[1];
                
                    List<BlockSelector> filteredSelectors = config.Selectors
                        .Where(predicate)
                        .ToList();
                
                    Func<BlockPos, BlockPos, List<WayPoint>, Block, BlockPos, bool> onBlock =
                        (mapMiddlePos, playerPos, results, block, pos) =>
                        {
                            foreach(BlockSelector selector in filteredSelectors)
                            {
                                if (block.Code.Path.StartsWith(selector.StartsWith))
                                {
                                    string blockName = block.GetPlacedBlockName(api.World, pos);
                                    WayPoint wayPoint = new WayPoint(block.Code.Path, pos.Copy(), blockName, selector.Icon, selector.Color); 
                                    results.Add(wayPoint);
                                    return true;
                                }
                            }

                            return true;
                        };
                    
                    return VsTutorialModSystem.ProcessFindBlock(api, saveFilePath, addWaypoints, radius, this.ClosestOnly, onBlock);
                });
            
        }
    }
    
    public class TranslocatorLocatorConfig
    {
        public static String ConfigName = "translocatorLocator.json";
        
        public int DefaultSearchRadius { get; set; } = 150;
        public List<BlockSelector> Selectors { get; set; } = new List<BlockSelector>();
        public List<LocatorCommand> Commands { get; set; } = new List<LocatorCommand>();

        public string GenerateCommandsTable()
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("<table>");
            sb.AppendLine("  <tr><th>Command</th><th>Description</th></tr>");
            sb.AppendLine("  <tr><td>.findtl</td><td>Finds nearby translocators.</td></tr>");

            foreach (var cmd in this.Commands)
            {
                sb.AppendLine($"  <tr><td>.{cmd.Name}</td><td>{cmd.Description}</td></tr>");
            }

            sb.AppendLine("</table>");
            return sb.ToString();
        }
        
        public static TranslocatorLocatorConfig GetDefault()
        {
            return new TranslocatorLocatorConfig
            {
                DefaultSearchRadius = 150,
                Selectors = new List<BlockSelector>
                {
                    new("tapestry", "vessel" , "blue", ["art", "treasure"]),
                    new("painting", "vessel" , "blue", ["art", "treasure"]), 
                    new("chandelier", "vessel" , "brown", ["art", "treasure"]), 
                    new("trunk", "vessel" , "brown", ["chest", "treasure"]), 
                    new("lootvessel", "vessel" , "brown", ["chest", "treasure"]), 
                    new("storagevessel", "vessel" , "brown", ["chest", "treasure"]), 
                    new("talldisplaycase", "vessel" , "brown", ["chest", "treasure"]), 
                    new("displaycase", "vessel" , "brown", ["chest", "treasure"]),
                    new("bonysoil", "rocks" , "brown", ["soil"]), 
                    new("soil-high", "rocks" , "brown", ["soil"]), 
                    new("soil-compost", "rocks" , "brown", ["soil"]), 
                    new("rawclay-fire", "rocks" , "orange", ["clay"]), 
                    new("rawclay-red", "rocks" , "red", ["clay"]), 
                    new("rawclay-blue", "rocks" , "blue", ["clay"]), 
                    new("looseores", "pick" , "yellow", ["ore"]), 
                    new("ore", "pick" , "red", ["ore"]), 
                    new("crystal", "rocks" , "black", ["ore"]), 
                    new("wildbeehives", "bee" , "yellow", ["bees"]), 
                    new("skeep", "bee" , "yellow", ["bees"]), 
                    new("log-resin", "tree" , "yellow", ["resin"]), 
                    new("mushroom", "mushroom" , "red", ["plant"]),
                    new("fruittree", "tree2" , "red", ["fruit"]),
                    new("tallplant-tule", "x" , "red", ["plant"]),
                    new("tallplant-coopersreed", "x" , "red", ["plant"]),
                    new("tallplant-papyrus", "x" , "red", ["plant"]),
                    new("bigberrybush", "berries" , "red", ["plant"]),
                    new("smallberrybush", "berries" , "red", ["plant"]),
                    new("flower", "x" , "red", ["plant"]),
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
                    new("findfruit", "Finds nearby fruit trees.", false, "fruit"),
                },
            };
        }
        
        public static TranslocatorLocatorConfig Load(ICoreClientAPI api)
        {
            TranslocatorLocatorConfig? config = null;
            
            try
            {
                config =  api.LoadModConfig<TranslocatorLocatorConfig>(TranslocatorLocatorConfig.ConfigName);
            }
            catch (Exception e)
            {
                api.Logger.Error("Failed to load mod config, using defaults. Error: " + e.Message);
            }
            
            if (config == null || config.Commands == null || config.Selectors == null)
            {
                api.Logger.Notification("Config missing or invalid, generating default...");
                config = TranslocatorLocatorConfig.GetDefault();
        
                // Save the clean default so the user has a valid file to edit
                api.StoreModConfig(config, TranslocatorLocatorConfig.ConfigName);
            }

            return config;
        }
    }
    
    public class VsTutorialModSystem : ModSystem
    {
        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Client;
        
        public override void StartClientSide(ICoreClientAPI api)
        {
            TranslocatorLocatorConfig config = TranslocatorLocatorConfig.Load(api);
            
            string saveFilePath = Path.Combine(GamePaths.DataPath, "ModData", "found_waypoints.json");
            
            api.ChatCommands.Create("findtl")
                .WithDescription("Finds nearby translocators.")
                // Added a boolean parser. Optional(false) makes it default to false if omitted.
                .WithArgs(
                    api.ChatCommands.Parsers.OptionalBool("addWaypoints"),
                    api.ChatCommands.Parsers.OptionalInt("radius", config.DefaultSearchRadius))
                .HandleWith(args => 
                {
                    bool addWaypoints = true.Equals(args[0]);
                    int radius =(int)args[1];
                    
                    return ProcessFindTranslocator(api, saveFilePath, addWaypoints, radius);
                });
            
            foreach(LocatorCommand command in config.Commands)
            {
                command.CreateCommand(api, saveFilePath, config);
            }
        }
        
        private static TextCommandResult ProcessFindTranslocator(ICoreClientAPI api, string saveFilePath, bool addWaypoints, int radius)
        {
            Func<BlockPos, BlockPos, List<WayPoint>, Block, BlockPos, bool> onBlock = (mapMiddlePos, playerPos, results, block, pos) =>
            {
                if (!block.Code.Path.StartsWith("statictranslocator"))
                {
                    return true;
                }

                bool isRepaired = false;

                if (block.Code.Path.StartsWith("statictranslocator-broken"))
                {
                    isRepaired = false;
                }
                else if(block.Code.Path.StartsWith("statictranslocator-normal"))
                {
                    isRepaired = true;
                }

                BlockPos destination = null;
                
                if (block is BlockStaticTranslocator translocatorBlock)
                {
                    isRepaired = translocatorBlock.Repaired;
                    api.Logger.Debug("[Translocator Locator] Found repaired status: {0} for {1}", translocatorBlock.Repaired, pos);
                    BlockEntity be = api.World.BlockAccessor.GetBlockEntity(pos);
                    if (be is BlockEntityStaticTranslocator translocator)
                    {
                        destination = translocator.TargetLocation;
                        api.Logger.Debug("[Translocator Locator] Found destination: {0} for {1}", destination, pos);
                    }
                }
                
                api.Logger.Debug("[Translocator Locator] Found translocator block: {0} at {1}", block.Code.Path, pos);
                
                WayPoint sourceWayPoint = new WayPoint(block.Code.Path, pos.Copy(), block.GetPlacedBlockName(api.World, pos),
                    "spiral", isRepaired ? "green" : "red");
                results.Add(sourceWayPoint);
                
                if (destination != null)
                {
                    // TODO: Technically this codepath is incorrect as it is the source's path
                    WayPoint destinationWayPoint = new WayPoint(block.Code.Path, destination.Copy(), block.GetPlacedBlockName(api.World, destination),
                        "spiral", "green");
                    results.Add(destinationWayPoint);
                    destinationWayPoint.ExtraChat = " to " + sourceWayPoint.ToRelativeCoordinates(mapMiddlePos, playerPos);
                    sourceWayPoint.ExtraChat = " to " + destinationWayPoint.ToRelativeCoordinates(mapMiddlePos, playerPos);
                }

                return true;
            };

            return ProcessFindBlock(api, saveFilePath, addWaypoints, radius, false, onBlock);
        }
        
        public static TextCommandResult ProcessFindBlock(ICoreClientAPI api, String saveFilePath, bool addWaypoints, int radius, bool closestOnly, Func<BlockPos, BlockPos, List<WayPoint>, Block, BlockPos, bool> onBlock)
        {
            if (api.World?.Player?.Entity == null)
            {
                return TextCommandResult.Error("Player not found.");
            }

            BlockPos playerPos = api.World.Player.Entity.Pos.AsBlockPos;
            BlockPos mapMiddlePos = api.World.DefaultSpawnPosition.AsBlockPos;
            List<WayPoint> results = new List<WayPoint>();
            Dictionary<string, HashSet<WayPoint>?> previousWayPoints = new();
            
            try {
                if (File.Exists(saveFilePath))
                {
                    string json = File.ReadAllText(saveFilePath);
                    var loaded = JsonConvert.DeserializeObject<Dictionary<string, HashSet<WayPoint>?>>(json);
                    if (loaded != null) previousWayPoints = loaded;
                }
            } catch (Exception e) {
                api.Logger.Error(e);
            }
            
            api.World.BlockAccessor.SearchBlocks(playerPos.AddCopy(-radius, -radius, -radius), playerPos.AddCopy(radius, radius, radius), (block, pos) =>
                onBlock(mapMiddlePos, playerPos, results, block, pos));

            IEnumerable<WayPoint> query = results
                .OrderBy(tl => tl.DistanceTo(playerPos)) ;
            
            if (closestOnly)
            {
                query = query.GroupBy(wp => wp.Name)
                    .Select(group => group.First());
            }

            List<WayPoint> sortedResults = query.Reverse().ToList();
            
            var checkWayPoints = previousWayPoints.Get(api.World.SavegameIdentifier);
            if (checkWayPoints == null)
            {
                checkWayPoints = new HashSet<WayPoint>();
                previousWayPoints[api.World.SavegameIdentifier] = checkWayPoints;
            }
            
            List<String> messages = new List<string>();
            foreach (var result in sortedResults)
            {
                messages.Add(result.ToChatString(mapMiddlePos, playerPos));
                if (addWaypoints)
                {
                    if (checkWayPoints.Contains(result))
                    {
                        api.Logger.Debug($"WayPoint already exists: {result.Pos}");
                    }
                    else
                    {
                        api.SendChatMessage(result.ToWaypointString());
                    }
                }
            }
            
            if (messages.Count > 0)
            {
                string fullMessage = string.Join("\n", messages);
                api.ShowChatMessage(fullMessage);

                if (addWaypoints)
                {
                    try {
                        string? folderPath = Path.GetDirectoryName(saveFilePath);
            
                        // 2. Create the directory if it doesn't exist
                        if (folderPath != null && !Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }
                    
                        checkWayPoints.UnionWith(sortedResults);
                    
                        string json = JsonConvert.SerializeObject(previousWayPoints, Formatting.Indented);
                        File.WriteAllText(saveFilePath, json);
                        api.Logger.Debug($"Saved {checkWayPoints.Count} waypoints to {saveFilePath}");
                        return TextCommandResult.Success($"Found {sortedResults.Count} waypoints within {radius} blocks.");
                    } catch (Exception e) {
                        return TextCommandResult.Error("Failed to save: " + e.Message);
                    }
                }
                else
                {
                    return TextCommandResult.Success($"Found {sortedResults.Count} waypoints within {radius} blocks.");
                }
            }
            
            return TextCommandResult.Success($"Not found within {radius} blocks.");
        }

    }
}