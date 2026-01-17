#nullable enable

using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using System.Linq;
using Vintagestory.API.Config;

namespace VSTutorial
{
    public class WayPoint
    {
        public BlockPos Pos { get; }
        public string Name { get; }
        public string Icon { get; }
        public string Color { get; }
        public string ExtraChat { get; set; }

        public WayPoint(BlockPos pos, string name, string icon, string color)
        {
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
    }
    
    public class VsTutorialModSystem : ModSystem
    {
        private ICoreClientAPI _api;
        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Client;

        public override void StartClientSide(ICoreClientAPI api)
        {
            this._api = api;
            
            api.ChatCommands.Create("findtl")
                .WithDescription("Finds nearby translocators.")
                // Added a boolean parser. Optional(false) makes it default to false if omitted.
                .WithArgs(
                    api.ChatCommands.Parsers.OptionalBool("addWaypoints"),
                    api.ChatCommands.Parsers.OptionalInt("radius", 150))
                .HandleWith(args => 
                {
                    bool addWaypoints = true.Equals(args[0]);
                    int radius =(int)args[1];
                    
                    return ProcessFindTranslocator(addWaypoints, radius);
                });
            
            api.ChatCommands.Create("findtreasure")
                .WithDescription("Finds nearby treasure.")
                // Added a boolean parser. Optional(false) makes it default to false if omitted.
                .WithArgs(
                    api.ChatCommands.Parsers.OptionalBool("addWaypoints"),
                    api.ChatCommands.Parsers.OptionalInt("radius", 150))
                .HandleWith(args => 
                {
                    bool addWaypoints = true.Equals(args[0]);
                    int radius =(int)args[1];
                    
                    string[] keys = { "tapestry", "painting", "trunk", "chest", "lootvessel", "chandelier", "storagevessel", "talldisplaycase", "displaycase"};

                    return ProcessFindStartsWith(addWaypoints, radius, keys);
                });
            
            api.ChatCommands.Create("findchest")
                .WithDescription("Finds nearby chests.")
                // Added a boolean parser. Optional(false) makes it default to false if omitted.
                .WithArgs(
                    api.ChatCommands.Parsers.OptionalBool("addWaypoints"),
                    api.ChatCommands.Parsers.OptionalInt("radius", 150))
                .HandleWith(args => 
                {
                    bool addWaypoints = true.Equals(args[0]);
                    int radius =(int)args[1];
                    
                    string[] keys = { "trunk", "chest", "lootvessel", "storagevessel", "talldisplaycase", "displaycase"};

                    return ProcessFindStartsWith(addWaypoints, radius, keys);
                });
            
            api.ChatCommands.Create("findart")
                .WithDescription("Finds nearby tapestries or paintings.")
                // Added a boolean parser. Optional(false) makes it default to false if omitted.
                .WithArgs(
                    api.ChatCommands.Parsers.OptionalBool("addWaypoints"),
                    api.ChatCommands.Parsers.OptionalInt("radius", 150))
                .HandleWith(args => 
                {
                    bool addWaypoints = true.Equals(args[0]);
                    int radius =(int)args[1];
                    
                    string[] keys = { "tapestry", "painting", "banner", "chandelier"};

                    return ProcessFindStartsWith(addWaypoints, radius, keys);
                });
            
            api.ChatCommands.Create("findbees")
                .WithDescription("Finds nearby wild beehives.")
                // Added a boolean parser. Optional(false) makes it default to false if omitted.
                .WithArgs(
                    api.ChatCommands.Parsers.OptionalBool("addWaypoints"),
                    api.ChatCommands.Parsers.OptionalInt("radius", 150))
                .HandleWith(args => 
                {
                    bool addWaypoints = true.Equals(args[0]);
                    int radius =(int)args[1];
                    
                    string[] keys = { "wildbeehives", "skeep" };

                    return ProcessFindStartsWith(addWaypoints, radius, keys);
                });
            
            api.ChatCommands.Create("findresin")
                .WithDescription("Finds resin trees.")
                // Added a boolean parser. Optional(false) makes it default to false if omitted.
                .WithArgs(
                    api.ChatCommands.Parsers.OptionalBool("addWaypoints"),
                    api.ChatCommands.Parsers.OptionalInt("radius", 150))
                .HandleWith(args => 
                {
                    bool addWaypoints = true.Equals(args[0]);
                    int radius =(int)args[1];
                    
                    string[] keys = { "log-resin" };

                    return ProcessFindStartsWith(addWaypoints, radius, keys);
                });
            
            // Should find closest like clay...
            /*
            api.ChatCommands.Create("findsoil")
                .WithDescription("Finds nearby boney soil or high fertility soil.")
                // Added a boolean parser. Optional(false) makes it default to false if omitted.
                .WithArgs(
                    api.ChatCommands.Parsers.OptionalBool("addWaypoints"),
                    api.ChatCommands.Parsers.OptionalInt("radius", 150))
                .HandleWith(args => 
                {
                    bool addWaypoints = true.Equals(args[0]);
                    int radius =(int)args[1];
                    
                    string[] keys = { "bonysoil", "soil-high", "soil-compost" };

                    return ProcessFindStartsWith(addWaypoints, radius, keys);
                });
            */
            api.ChatCommands.Create("findcrystal")
                .WithDescription("Finds nearby crystals.")
                // Added a boolean parser. Optional(false) makes it default to false if omitted.
                .WithArgs(
                    api.ChatCommands.Parsers.OptionalBool("addWaypoints"),
                    api.ChatCommands.Parsers.OptionalInt("radius", 150))
                .HandleWith(args => 
                {
                    bool addWaypoints = true.Equals(args[0]);
                    int radius =(int)args[1];
                    
                    string[] keys = { "crystal" };

                    return ProcessFindStartsWith(addWaypoints, radius, keys);
                });
            /*
            api.ChatCommands.Create("findclay")
                .WithDescription("Finds nearby boney soil or high fertility soil.")
                // Added a boolean parser. Optional(false) makes it default to false if omitted.
                .WithArgs(
                    api.ChatCommands.Parsers.OptionalBool("addWaypoints"),
                    api.ChatCommands.Parsers.OptionalInt("radius", 150))
                .HandleWith(args => 
                {
                    bool addWaypoints = true.Equals(args[0]);
                    int radius =(int)args[1];
                    
                    string[] keys = { "rawclay-fire", "rawclay-red", "rawclay-blue" };

                    return ProcessFindStartsWith(addWaypoints, radius, keys);
                });
                */            
        }

        private TextCommandResult ProcessFindStartsWith(bool addWaypoints, int radius, string[] keys)
        {
            Func<BlockPos, BlockPos, List<WayPoint>, Block, BlockPos, bool> onBlock =
                (mapMiddlePos, playerPos, results, block, pos) =>
                {
                    foreach(String key in keys)
                    {
                        if (block.Code.Path.StartsWith(key))
                        {
                            string blockName = block.GetPlacedBlockName(_api.World, pos);
                            WayPoint wayPoint = new WayPoint(pos.Copy(), blockName, "vessel", "yellow");
                            results.Add(wayPoint);
                            return true;
                        }
                    }

                    return true;
                };
            
            return ProcessFindBlock(addWaypoints, radius, onBlock);
        }
        
        private TextCommandResult ProcessFindTranslocator(bool addWaypoints, int radius)
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
                    _api.Logger.Debug("[Translocator Locator] Found repaired status: {0} for {1}", translocatorBlock.Repaired, pos);
                    BlockEntity be = _api.World.BlockAccessor.GetBlockEntity(pos);
                    if (be is BlockEntityStaticTranslocator translocator)
                    {
                        destination = translocator.TargetLocation;
                        _api.Logger.Debug("[Translocator Locator] Found destination: {0} for {1}", destination, pos);
                    }
                }
                
                _api.Logger.Debug("[Translocator Locator] Found translocator block: {0} at {1}", block.Code.Path, pos);
                
                WayPoint sourceWayPoint = new WayPoint(pos.Copy(), block.GetPlacedBlockName(_api.World, pos),
                    "spiral", isRepaired ? "green" : "red");
                results.Add(sourceWayPoint);
                
                if (destination != null)
                {
                    WayPoint destinationWayPoint = new WayPoint(destination.Copy(), block.GetPlacedBlockName(_api.World, destination),
                        "spiral", "green");
                    results.Add(destinationWayPoint);
                    destinationWayPoint.ExtraChat = " to " + sourceWayPoint.ToRelativeCoordinates(mapMiddlePos, playerPos);
                    sourceWayPoint.ExtraChat = " to " + destinationWayPoint.ToRelativeCoordinates(mapMiddlePos, playerPos);
                }

                return true;
            };

            return ProcessFindBlock(addWaypoints, radius, onBlock);
        }
        
        private TextCommandResult ProcessFindBlock(bool addWaypoints, int radius, Func<BlockPos, BlockPos, List<WayPoint>, Block, BlockPos, bool> onBlock)
        {
            if (_api.World?.Player?.Entity == null)
            {
                return TextCommandResult.Error("Player not found.");
            }

            BlockPos playerPos = _api.World.Player.Entity.Pos.AsBlockPos;
            BlockPos mapMiddlePos = _api.World.DefaultSpawnPosition.AsBlockPos;
            List<WayPoint> results = new List<WayPoint>();
            
            _api.World.BlockAccessor.SearchBlocks(playerPos.AddCopy(-radius, -radius, -radius), playerPos.AddCopy(radius, radius, radius), (block, pos) =>
            {
                return onBlock(mapMiddlePos, playerPos, results, block, pos);
            });

            List<WayPoint> sortedResults = results
                .OrderByDescending(tl => tl.DistanceTo(playerPos))
                .ToList(); 
            
            List<String> messages = new List<string>();
            foreach (var result in sortedResults)
            {
                messages.Add(result.ToChatString(mapMiddlePos, playerPos));
                if (addWaypoints)
                {
                    _api.SendChatMessage(result.ToWaypointString());
                }
            }
            
            if (messages.Count > 0)
            {
                string fullMessage = "Found:\n" + string.Join("\n", messages);
                _api.ShowChatMessage(fullMessage);
                return TextCommandResult.Success("");
            }
            
            return TextCommandResult.Success($"Not found within {radius} blocks.");
        }

    }
}