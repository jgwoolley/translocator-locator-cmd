#nullable enable

using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using System.Linq;

namespace VSTutorial
{
    public class RelativePointDistance
    {
        public BlockPos MapMiddle { get; }
        public BlockPos Pos { get; }
        public int RelativeX => Pos.X - MapMiddle.X;
        public int RelativeY => Pos.Y;
        public int RelativeZ => Pos.Z - MapMiddle.Z;

        public RelativePointDistance(BlockPos mapMiddle, BlockPos pos)
        {
            MapMiddle = mapMiddle;
            Pos = pos;
        }

        public double DistanceTo(BlockPos other)
        {
            return Math.Sqrt(Pos.DistanceSqTo(other.X, other.Y, other.Z));
        }
        
        public string ToWaypointString(string wpColor, string wpName)
        {
            return $"/waypoint addati spiral ={Pos.X} ={Pos.Y} ={Pos.Z} false {wpColor} {wpName}";
        }
    }
    
    public class TranslocatorResult
    {
        public BlockPos PlayerPos;
        public bool IsRepaired { get;  }
        public RelativePointDistance Source { get; }
        public RelativePointDistance? Destination { get; }
        public int Distance => (int)Source.DistanceTo(PlayerPos);
        
        public TranslocatorResult(BlockPos mapMiddle, BlockPos source, BlockPos? destination, BlockPos playerPos, bool isRepaired)
        {
            Source = new RelativePointDistance(mapMiddle, source);
            Destination = destination == null ? null: new RelativePointDistance(mapMiddle, destination);
            PlayerPos = playerPos;
            IsRepaired = isRepaired;
        }
        
        public string ToChatString()
        {
            string color = IsRepaired ? "#77ff77" : "#ff7777";
            string status = IsRepaired ? "ACTIVE" : "BROKEN";
            
            string destinationResult = Destination == null ? "": $" → {Destination.RelativeX}, {Destination.RelativeY}, {Destination.RelativeZ} ({(int)Destination.DistanceTo(PlayerPos)}m away)";
            
            return $"<font color=\"{color}\">[{status}]</font> " +
                   $"at <strong>{Source.RelativeX}, {Source.RelativeY}, {Source.RelativeZ} ({Distance}m away){destinationResult}</strong>";
        }

        public string SourceToWaypointString()
        {
            string wpColor = IsRepaired ? "green" : "red";
            string wpName = IsRepaired ? "Active Translocator" : "Broken Translocator";

            return Source.ToWaypointString(wpColor, wpName);
        }
        
        public string DestinationToWaypointString()
        {
            if (Destination == null)
            {
                return "";
            }
            return Destination.ToWaypointString("green", "Active Translocator");
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
        }

        private TextCommandResult ProcessFindTranslocator(bool addWaypoints, int radius)
        {
            if (_api.World?.Player?.Entity == null)
            {
                return TextCommandResult.Error("Player not found.");
            }

            BlockPos playerPos = _api.World.Player.Entity.Pos.AsBlockPos;
            BlockPos mapMiddle = _api.World.DefaultSpawnPosition.AsBlockPos;
            List<TranslocatorResult> results = new List<TranslocatorResult>();
            
            _api.World.BlockAccessor.SearchBlocks(playerPos.AddCopy(-radius, -radius, -radius), playerPos.AddCopy(radius, radius, radius), (block, pos) =>
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
                
                TranslocatorResult result = new TranslocatorResult(mapMiddle, pos.Copy(), destination?.Copy(), playerPos.Copy(), isRepaired);
                results.Add(result);
                    
                return true; 
            });

            List<TranslocatorResult> sortedResults = results
                .OrderByDescending(tl => tl.Distance)
                .ToList(); 
            
            List<String> messages = new List<string>();
            foreach (var result in sortedResults)
            {
                if (addWaypoints)
                {
                    _api.SendChatMessage(result.SourceToWaypointString());

                    if (result.Destination != null)
                    {
                        _api.SendChatMessage(result.DestinationToWaypointString());
                    }
                }
                messages.Add(result.ToChatString());

            }
            
            if (messages.Count > 0)
            {
                return TextCommandResult.Success("Found:\n" + string.Join("\n", messages));
            }
            
            return TextCommandResult.Success($"No translocators found within {radius} blocks.");
        }
    }
}