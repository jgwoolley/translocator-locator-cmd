using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

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
            if (capi.World?.Player?.Entity == null) 
                return TextCommandResult.Error("Player not found.");

            BlockPos playerPos = capi.World.Player.Entity.Pos.AsBlockPos;
            List<string> results = new List<string>();

            // We scan a 160x160 area around the player (roughly 10 chunks)
            // Scanning every single block is slow, so we only check Block Entities
            int radius = 80; 

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -64; y <= 64; y++) // Search 64 blocks up and down
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        BlockPos tmpPos = new BlockPos(playerPos.X + x, playerPos.Y + y, playerPos.Z + z, playerPos.dimension);
                        
                        // GetBlockEntity is the most basic, stable method in the API
                        BlockEntity be = capi.World.BlockAccessor.GetBlockEntity(tmpPos);
                        
                        if (be != null && be.GetType().Name == "BEStaticTranslocator")
                        {
                            int dist = (int)tmpPos.DistanceTo(playerPos);
                            
                            // Reflection to avoid "Namespace Not Found" errors
                            bool repaired = false;
                            var repProp = be.GetType().GetProperty("Repaired");
                            if (repProp != null) repaired = (bool)repProp.GetValue(be);

                            string color = repaired ? "#77ff77" : "#ff7777";
                            string status = repaired ? "ACTIVE" : "BROKEN";

                            string msg = $"<strong><font color=\"{color}\">[{status}]</font></strong> at {tmpPos.X}, {tmpPos.Y}, {tmpPos.Z} ({dist}m)";

                            if (repaired)
                            {
                                var destProp = be.GetType().GetProperty("tpDestination");
                                if (destProp != null && destProp.GetValue(be) is BlockPos dest)
                                {
                                    int travel = (int)tmpPos.DistanceTo(dest);
                                    msg += $"\n   <font color=\"#aaaaaa\">└─ Leads to: {dest.X}, {dest.Y}, {dest.Z} ({travel}m away)</font>";
                                }
                            }

                            results.Add(msg);
                        }
                    }
                }
            }

            if (results.Count > 0)
            {
                return TextCommandResult.Success("Found:\n" + string.Join("\n", results));
            }
            
            return TextCommandResult.Success("No translocators found in immediate vicinity (80 block radius).");
        }
    }
}