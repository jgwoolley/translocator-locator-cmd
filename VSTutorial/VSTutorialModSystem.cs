using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;

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
                .WithDescription("Finds nearby translocators")
                .HandleWith(args => 
                {
                    return ProcessFindTL();
                });
        }

      private TextCommandResult ProcessFindTL()
        {
            if (capi.World?.Player?.Entity == null) {
                return TextCommandResult.Error("Player not found.");
            }

            // The raw "Global" coordinates
            BlockPos pPos = capi.World.Player.Entity.Pos.AsBlockPos;
            
            // The offset used by the game to show "User Friendly" coordinates
            BlockPos mapMiddle = capi.World.DefaultSpawnPosition.AsBlockPos;

            List<string> results = new List<string>();
            int radius = 150;
            
            HashSet<int> tlIds = new HashSet<int>();
            foreach (var block in capi.World.Blocks)
            {
                if (block?.Code != null && block.Code.Path.Contains("statictranslocator"))
                {
                    tlIds.Add(block.Id);
                }
            }

            // Define the search area
            BlockPos minPos = pPos.AddCopy(-radius, -radius, -radius);
            BlockPos maxPos = pPos.AddCopy(radius, radius, radius);

            // SearchBlocks is much faster than nested for-loops
            capi.World.BlockAccessor.SearchBlocks(minPos, maxPos, (block, pos) =>
            {
                // Only process if it's a static translocator
                if (block.Code.Path.Contains("statictranslocator"))
                {
                    // Logic: Repaired translocators have 'repaired' or 'active' in the name
                    // Broken ones usually have 'broken' or just the base name.
                    bool isRepaired = block.Code.Path.Contains("normal") || block.Code.Path.Contains("repaired") || 
                        block.Code.Path.Contains("active");

                    int userX = pos.X - mapMiddle.X;
                    int userY = pos.Y;
                    int userZ = pos.Z - mapMiddle.Z;
                    double dist = Math.Sqrt(pos.DistanceSqTo(pPos.X, pPos.Y, pPos.Z));
                    string status = isRepaired ? "ACTIVE" : "BROKEN";
                    string color = isRepaired ? "#77ff77" : "#ff7777";

                    results.Add($"<font color=\"{color}\">[{status}]</font> at <strong>{userX}, {userY}, {userZ}</strong> ({(int)dist}m away)");
                }
                return true; // Keep searching
            });

            if (results.Count > 0)
                return TextCommandResult.Success("Found:\n" + string.Join("\n", results));
            
            return TextCommandResult.Success($"No translocators found near your map coordinates.");
        }
    }
}