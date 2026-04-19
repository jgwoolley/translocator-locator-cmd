using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.Sponge;

public class ItemSponge : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            IWorldAccessor world = byEntity.World;
            if (world.Api.Side != EnumAppSide.Server) return;

            string state = slot.Itemstack?.ItemAttributes?["spongeState"]?.AsString("dry") ?? "dry";

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

            int radius = SpongeModSystem.Config?.AbsorbRadius ?? 1;
            radius = GameMath.Clamp(radius, 0, 16);

            int removed = AbsorbSourceWater(world, blockSel.Position, radius);

            if (removed > 0)
            {
                // Become wet after successful use
                if (TrySetSponge(slot, world, "nf3tsponge:sponge-wet"))
                {
                    handHandling = EnumHandHandling.PreventDefault;
                }
            }
        }

        private int AbsorbSourceWater(IWorldAccessor world, BlockPos center, int radius)
        {
            int removed = 0;
            Block air = world.GetBlock(new AssetLocation("air"));
            BlockPos tmp = new BlockPos();

            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            for (int dz = -radius; dz <= radius; dz++)
            {
                tmp.Set(center.X + dx, center.Y + dy, center.Z + dz);

                Block b = world.BlockAccessor.GetBlock(tmp);
                if (IsSourceWater(b))
                {
                    world.BlockAccessor.SetBlock(air.BlockId, tmp);
                    removed++;
                }
            }

            return removed;
        }

        private bool IsSourceWater(Block block)
        {
            if (block == null) return false;

            // 1) Must be a liquid block identified as water
            // LiquidCode is a string in your API version.
            string code = block.LiquidCode;
            if (string.IsNullOrEmpty(code)) return false;

            // Most commonly "water". If you want other liquids, add them here.
            if (!(code.Equals("water", StringComparison.OrdinalIgnoreCase)
                  || code.Equals("saltwater", StringComparison.OrdinalIgnoreCase))) return false;
            
            // 2) Only source blocks: check liquid level variants.
            // In VS assets, the "full/source" liquid level is usually the max value.
            string lvlStr = null;

            if (block.Variant != null)
            {
                block.Variant.TryGetValue("liquidlevel", out lvlStr);
                if (lvlStr == null) block.Variant.TryGetValue("level", out lvlStr);
            }

            if (lvlStr == null) return false;
            if (!int.TryParse(lvlStr, out int lvl)) return false;

            // Treat the highest levels as "source". (Commonly 7 is max/full.)
            return lvl >= 7;
        }

        private bool TrySetSponge(ItemSlot slot, IWorldAccessor world, string assetCode)
        {
            // assetCode example: "nf3tsponge:sponge-wet"
            Item spongeItem = world.GetItem(new AssetLocation(assetCode));
            if (spongeItem == null) return false;

            slot.Itemstack = new ItemStack(spongeItem);
            slot.MarkDirty();
            return true;
        }
    }