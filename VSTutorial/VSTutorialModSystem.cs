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

    // The raw "Global" coordinates
    BlockPos pPos = capi.World.Player.Entity.Pos.AsBlockPos;
    
    // The offset used by the game to show "User Friendly" coordinates
    BlockPos mapMiddle = capi.World.DefaultSpawnPosition.AsBlockPos;

    List<string> results = new List<string>();
    int radius = 80;

    HashSet<int> tlIds = new HashSet<int>();
    foreach (var block in capi.World.Blocks)
    {
        if (block?.Code != null && block.Code.Path.Contains("statictranslocator"))
        {
            tlIds.Add(block.Id);
        }
    }

    BlockPos tmpPos = new BlockPos(0, 0, 0, pPos.dimension);

    for (int x = -radius; x <= radius; x++)
    {
        for (int z = -radius; z <= radius; z++)
        {
            for (int y = -20; y <= 20; y++) 
            {
                tmpPos.Set(pPos.X + x, pPos.Y + y, pPos.Z + z);
                int blockId = capi.World.BlockAccessor.GetBlockId(tmpPos);
                
                if (tlIds.Contains(blockId))
                {
                    // Convert World -> User/Map Coordinates
                    int userX = tmpPos.X - mapMiddle.X;
                    int userY = tmpPos.Y; // Y is usually absolute
                    int userZ = tmpPos.Z - mapMiddle.Z;

                    double dist = tmpPos.DistanceTo(pPos);
                    
                    // Attempt to get repair status
                    BlockEntity be = capi.World.BlockAccessor.GetBlockEntity(tmpPos);
                    bool repaired = false;
                    if (be != null)
                    {
                        var field = be.GetType().GetField("repaired", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                 ?? be.GetType().GetField("Repaired", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (field != null) repaired = (bool)field.GetValue(be);
                    }

                    string status = repaired ? "ACTIVE" : "BROKEN";
                    string color = repaired ? "#77ff77" : "#ff7777";

                    results.Add($"<font color=\"{color}\">[{status}]</font> at <strong>{userX}, {userY}, {userZ}</strong> ({(int)dist}m away)");
                }
            }
        }
    }

    if (results.Count > 0)
        return TextCommandResult.Success("Found:\n" + string.Join("\n", results));
    
    return TextCommandResult.Success($"No translocators found near your map coordinates.");
}

    }
}