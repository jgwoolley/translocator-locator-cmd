using System;
using System.Linq;
using System.Text.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.Sponge;

public class ItemSponge : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            
            var world = byEntity.World;
            var state = slot.Itemstack?.ItemAttributes?["spongeState"]?.AsString("dry") ?? "dry";
            
            // Always prevent default so the interaction is treated as handled
            handHandling = EnumHandHandling.PreventDefault;
            
            if (world.Api.Side != EnumAppSide.Server) return;
            
            if (state == "wet")
            {
                // Wring out: convert to dry sponge
                if (TrySetSponge(slot, world, "nf3tsponge:sponge-dry"))
                {
                    handHandling = EnumHandHandling.PreventDefault;
                }
                return;
            }

            // Dry sponge: must have a target block position
            if (blockSel == null) return;

            var radius = SpongeModSystem.Config?.AbsorbRadius ?? 1;
            radius = GameMath.Clamp(radius, 0, 16);
            
            var removed = 0;
            
            if (byEntity is EntityPlayer ep)
            {
                removed = AbsorbSourceWater(world, ep.Player, blockSel.Position, radius);
            }
            else
            {
                world.Api.Logger.Warning($"Could not get EntityPlayer from {byEntity}");
            }
            
            if (removed <= 0)
            {
                handHandling = EnumHandHandling.PreventDefault;
                return;
            }
            
            // Become wet after successful use
            if (TrySetSponge(slot, world, "nf3tsponge:sponge-wet"))
            {
                handHandling = EnumHandHandling.PreventDefault;
            }
        }
        
        private static int AbsorbSourceWater(IWorldAccessor world, IPlayer byPlayer, BlockPos center, int radius)
        {
            var removed = 0;
            
            var tmp = new BlockPos(center.dimension);

            var ba = world.BlockAccessor;
            
            for (var dx = -radius; dx <= radius; dx++)
            for (var dy = -radius; dy <= radius; dy++)
            for (var dz = -radius; dz <= radius; dz++)
            {
                tmp.Set(center.X + dx, center.Y + dy, center.Z + dz);
                
                var b = ba.GetBlock(tmp, BlockLayersAccess.Fluid);
                if (!IsSourceWater(b)) continue;

                ba.SetBlock(0, tmp, BlockLayersAccess.Fluid);
                ba.TriggerNeighbourBlockUpdate(tmp); // helps liquid reflow/update
                
                removed++;
            }

            ba.Commit();
            
            return removed;
        }

        private static bool IsSourceWater(Block block)
        {
            if (block == null) return false;

            var liquid = block.LiquidCode;
            if (string.IsNullOrEmpty(liquid)) return false;

            return liquid.Equals("water", StringComparison.OrdinalIgnoreCase)
                   || liquid.Equals("saltwater", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TrySetSponge(ItemSlot slot, IWorldAccessor world, string assetCode)
        {
            var spongeItem = world.GetItem(new AssetLocation(assetCode));
            if (spongeItem == null) return false;

            slot.Itemstack = new ItemStack(spongeItem);
            slot.MarkDirty();
            return true;
        }
    }