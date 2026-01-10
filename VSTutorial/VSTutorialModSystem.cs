using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using System.Linq;

namespace VSTutorial
{
    public class TranslocatorResult
    {
        public bool IsRepaired { get;  }
        public double Distance { get; }
        public int UserX { get; }
        public int UserY { get; }
        public int UserZ { get; }
        

        public TranslocatorResult(bool isRepaired, double distance, int userX, int userY, int userZ)
        {
            this.IsRepaired = isRepaired;
            this.Distance = distance;
            this.UserX = userX;
            this.UserY = userY;
            this.UserZ = userZ;
        }
        
        public string ToChatString()
        {
            string color = IsRepaired ? "#77ff77" : "#ff7777";
            string status = IsRepaired ? "ACTIVE" : "BROKEN";
            return $"<font color=\"{color}\">[{status}]</font> " +
                   $"at <strong>{UserX}, {UserY}, {UserZ}</strong> ({(int)Distance}m away)";
        }

        public string ToWaypointString()
        {
            string wpColor = IsRepaired ? "green" : "red";
            string wpName = IsRepaired ? "Active Translocator" : "Broken Translocator";
            return $"/waypoint addati spiral {UserX} {UserY} {UserZ} false {wpColor} {wpName}";
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

            BlockPos pPos = _api.World.Player.Entity.Pos.AsBlockPos;
            BlockPos mapMiddle = _api.World.DefaultSpawnPosition.AsBlockPos;
            List<TranslocatorResult> results = new List<TranslocatorResult>();
            
            _api.World.BlockAccessor.SearchBlocks(pPos.AddCopy(-radius, -radius, -radius), pPos.AddCopy(radius, radius, radius), (block, pos) =>
            {
                if (!block.Code.Path.StartsWith("statictranslocator"))
                {
                    return true;
                }
                
                _api.Logger.Debug("[Translocator Locator] Found translocator block: {0} at {1}", block.Code.Path, pos);
                
                bool isRepaired = !block.Code.Path.StartsWith("statictranslocator-broken") && block.Code.Path.StartsWith("statictranslocator-normal");
                
                // Define display variables
                int userX = pos.X - mapMiddle.X;
                int userY = pos.Y;
                int userZ = pos.Z - mapMiddle.Z;
                double dist = Math.Sqrt(pos.DistanceSqTo(pPos.X, pPos.Y, pPos.Z));
                
                TranslocatorResult result = new TranslocatorResult(isRepaired, dist, userX, userY, userZ);
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
                    _api.SendChatMessage(result.ToWaypointString());
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