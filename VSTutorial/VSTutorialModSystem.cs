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
    int radius = 80;

    // 1. Find the internal ID for translocators to avoid string comparisons in the loop
    // We look for any block that contains "statictranslocator"
    List<int> tlIds = new List<int>();
    foreach (var block in capi.World.Blocks)
    {
        if (block?.Code != null && block.Code.Path.Contains("statictranslocator"))
        {
            tlIds.Add(block.Id);
        }
    }

    if (tlIds.Count == 0) return TextCommandResult.Error("Translocator block type not found in registry.");

    // 2. Scan the area
    BlockPos tmpPos = new BlockPos();
    for (int x = -radius; x <= radius; x++)
    {
        for (int y = -30; y <= 30; y++) // Translocators are usually near floor level
        {
            for (int z = -radius; z <= radius; z++)
            {
                tmpPos.Set(playerPos.X + x, playerPos.Y + y, playerPos.Z + z);
                
                int blockId = capi.World.BlockAccessor.GetBlockId(tmpPos);
                
                if (tlIds.Contains(blockId))
                {
                    double dist = tmpPos.DistanceTo(playerPos);
                    
                    // Attempt to get the BlockEntity for status, but don't rely on it for the find
                    BlockEntity be = capi.World.BlockAccessor.GetBlockEntity(tmpPos);
                    string status = "FOUND";
                    string color = "#ffff77";

                    if (be != null)
                    {
                        var field = be.GetType().GetField("repaired", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                 ?? be.GetType().GetField("Repaired", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        
                        if (field != null)
                        {
                            bool repaired = (bool)field.GetValue(be);
                            status = repaired ? "ACTIVE" : "BROKEN";
                            color = repaired ? "#77ff77" : "#ff7777";
                        }
                    }

                    results.Add($"<strong><font color=\"{color}\">[{status}]</font></strong> at {tmpPos.X}, {tmpPos.Y}, {tmpPos.Z} ({(int)dist}m)");
                }
            }
        }
    }

    if (results.Count > 0)
        return TextCommandResult.Success("Found:\n" + string.Join("\n", results));
    
    return TextCommandResult.Success($"No translocators found in {radius} block radius.");
}

    }
}