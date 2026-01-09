using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VSTutorial
{
    public class VSTutorialModSystem : ModSystem
    {
        private ICoreClientAPI capi;

        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Client;

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;

            api.ChatCommands.Create("findtl")
                .WithDescription("Finds nearby translocators. Usage: .findtl [addWaypoints(true/false)]")
                // Added a boolean parser. Optional(false) makes it default to false if omitted.
                .WithArgs(api.ChatCommands.Parsers.OptionalBool("addWaypoints", "false"))
                .HandleWith(args => 
                {
                    bool shouldAddWaypoints = (bool)args[0];
                    return ProcessFindTL(shouldAddWaypoints);
                });
        }

        private TextCommandResult ProcessFindTL(bool addWaypoints)
        {
            if (capi.World?.Player?.Entity == null) 
                return TextCommandResult.Error("Player not found.");

            BlockPos pPos = capi.World.Player.Entity.Pos.AsBlockPos;
            BlockPos mapMiddle = capi.World.DefaultSpawnPosition.AsBlockPos;
            List<string> clientCommands = new List<string>();
            List<string> results = new List<string>();
            int radius = 150;

            var mapManager = capi.ModLoader.GetModSystem<WorldMapManager>();
            var waypointLayer = mapManager?.MapLayers.OfType<WaypointMapLayer>().FirstOrDefault();

            capi.World.BlockAccessor.SearchBlocks(pPos.AddCopy(-radius, -radius, -radius), pPos.AddCopy(radius, radius, radius), (block, pos) =>
            {
                if (block.Code.Path.Contains("statictranslocator"))
                {
                    bool isRepaired = !block.Code.Path.Contains("broken") && 
                                     (block.Code.Path.Contains("normal") || 
                                      block.Code.Path.Contains("repaired") || 
                                      block.Code.Path.Contains("active"));

                    // Define display variables
                    int userX = pos.X - mapMiddle.X;
                    int userY = pos.Y;
                    int userZ = pos.Z - mapMiddle.Z;
                    double dist = Math.Sqrt(pos.DistanceSqTo(pPos.X, pPos.Y, pPos.Z));
                    string status = isRepaired ? "ACTIVE" : "BROKEN";
                    string color = isRepaired ? "#77ff77" : "#ff7777";

                    if (addWaypoints)
                    {
                        string wpColor = isRepaired ? "green" : "red";
                        string wpName = isRepaired ? "Active Translocator" : "Broken Translocator";
                        capi.SendChatMessage($"/waypoint addati spiral ={pos.X} ={pos.Y} ={pos.Z} false {wpColor} {wpName}");
                    }
                    
                    results.Add($"<font color=\"{color}\">[{status}]</font> at <strong>{userX}, {userY}, {userZ}</strong> ({(int)dist}m away)");
                }
                return true; 
            });

            if (results.Count > 0)
                return TextCommandResult.Success("Found:\n" + string.Join("\n", results));
            
            return TextCommandResult.Success($"No translocators found within {radius} blocks.");
        }
    }
}