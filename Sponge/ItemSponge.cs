#nullable enable
using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.Sponge;

public class ItemSponge : Item
{
    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel,
        EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
    {
        handHandling = EnumHandHandling.PreventDefault;

        var world = byEntity.World;
        var state = slot.Itemstack?.ItemAttributes?["spongeState"]?.AsString("dry") ?? "dry";

        if (world.Api.Side != EnumAppSide.Server) return;

        // Dry sponge: must have a target block position
        if (blockSel == null)
        {
            world.Api.Logger.Warning("Could not get BlockSelection");
            return;
        }

        if (state == "wet")
        {
            // Wring out: convert to dry sponge
            if (TrySetSponge(slot, world, "nf3tsponge:sponge-dry"))
            {
                SpongeModSystem.SendFxToPlayer(byEntity, blockSel);
                MakeSplashServer(world, blockSel);
            }

            return;
        }

        var radius = SpongeModSystem.Config?.AbsorbRadius ?? 1;
        radius = GameMath.Clamp(radius, 0, 16);

        var removed = AbsorbSourceWater(world, blockSel.Position, radius);

        if (removed <= 0) return;

        // Become wet after successful use
        if (TrySetSponge(slot, world, "nf3tsponge:sponge-wet"))
        {
            SpongeModSystem.SendFxToPlayer(byEntity, blockSel);
            MakeSplashServer(world, blockSel);
        }
    }

    /// <summary>
    ///     Plays a splash sound near the selected block
    /// </summary>
    /// <param name="world"></param>
    /// <param name="blockSel"></param>
    private static void MakeSplashServer(IWorldAccessor world, BlockSelection blockSel)
    {
        if (blockSel == null) return;
        var x = blockSel.Position.X;
        var y = blockSel.Position.Y;
        var z = blockSel.Position.Z;

        var splashSoundAssetLocation = new AssetLocation("game:sounds/environment/smallsplash");
        world.PlaySoundAt(splashSoundAssetLocation, x + 0.5, y + 0.5, z + 0.5, range: 16f);
    }

    /// <summary>
    ///     Absorb any liquid in an area around the selected block
    /// </summary>
    /// <param name="world"></param>
    /// <param name="center"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    private static int AbsorbSourceWater(IWorldAccessor world, BlockPos center, int radius)
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

    /// <summary>
    ///     Checks if the block is water or not
    /// </summary>
    /// <param name="block"></param>
    /// <returns></returns>
    private static bool IsSourceWater(Block block)
    {
        if (block == null) return false;

        var liquid = block.LiquidCode;
        if (string.IsNullOrEmpty(liquid)) return false;

        return liquid.Equals("water", StringComparison.OrdinalIgnoreCase)
               || liquid.Equals("saltwater", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Triggers an update to the sponge to change its item
    /// </summary>
    /// <param name="slot"></param>
    /// <param name="world"></param>
    /// <param name="assetCode"></param>
    /// <returns></returns>
    private static bool TrySetSponge(ItemSlot slot, IWorldAccessor world, string assetCode)
    {
        var spongeItem = world.GetItem(new AssetLocation(assetCode));
        if (spongeItem == null) return false;

        slot.Itemstack = new ItemStack(spongeItem);
        slot.MarkDirty();
        return true;
    }
}