#nullable enable

using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Nf3t.VintageStory.TapestryRepair;

public class BlockRepairableTapestry : BlockTapestry
{
    // BlockEntityTapestry exposes Rotten read-only, so the backing field is set via reflection.
    private static readonly FieldInfo? RottenField =
        typeof(BlockEntityTapestry).GetField("rotten", BindingFlags.Instance | BindingFlags.NonPublic);

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityTapestry;
        var slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (be is { Rotten: false } && RottenField != null && ItemTemporalDecayKit.IsKit(slot)
            && world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
        {
            if (world.Side == EnumAppSide.Server)
            {
                RottenField.SetValue(be, true);
                if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
                {
                    slot.Itemstack!.Collectible.DamageItem(world, byPlayer.Entity, slot, ItemTemporalSewingKit.DurabilityPerRepair);
                    slot.MarkDirty();
                }

                be.MarkDirty(true);
                world.PlaySoundAt(new AssetLocation("sounds/block/cloth"), blockSel.Position, 0.5, byPlayer);
            }

            return true;
        }

        if (be is not { Rotten: true } || RottenField == null || !ItemTemporalSewingKit.IsKit(slot)
            || !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            return base.OnBlockInteractStart(world, byPlayer, blockSel);

        var creative = byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative;

        if (world.Side == EnumAppSide.Server)
        {
            RottenField.SetValue(be, false);
            if (!creative)
            {
                slot.Itemstack!.Collectible.DamageItem(world, byPlayer.Entity, slot, ItemTemporalSewingKit.DurabilityPerRepair);
                slot.MarkDirty();
            }

            be.MarkDirty(true);
            world.PlaySoundAt(new AssetLocation("sounds/block/cloth"), blockSel.Position, 0.5, byPlayer);
            world.PlaySoundAt(new AssetLocation("sounds/effect/gears"), blockSel.Position, 0.5, byPlayer, true, 16, 0.4f);
        }

        return true;
    }

    public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
    {
        var info = base.GetPlacedBlockInfo(world, pos, forPlayer);
        if (world.BlockAccessor.GetBlockEntity(pos) is not BlockEntityTapestry { Rotten: true }) return info;
        // The base info is trimmed, so it has no trailing newline to separate our line from it.
        var hint = Lang.Get("nf3ttapestryrepair:tapestry-repair-hint");
        return string.IsNullOrEmpty(info) ? hint : info.TrimEnd() + "\n" + hint;
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection,
        IPlayer forPlayer)
    {
        var help = base.GetPlacedBlockInteractionHelp(world, selection, forPlayer) ?? [];
        var tapestry = world.BlockAccessor.GetBlockEntity(selection.Position) as BlockEntityTapestry;
        if (tapestry is { Rotten: false }
            && world.GetItem(new AssetLocation(ItemTemporalDecayKit.KitCode)) is { } decay)
        {
            return help.Append(new WorldInteraction
            {
                ActionLangCode = "nf3ttapestryrepair:blockhelp-tapestry-decay-1",
                Itemstacks = [new ItemStack(decay)],
                MouseButton = EnumMouseButton.Right
            });
        }

        if (tapestry is not { Rotten: true })
            return help;

        var kit = world.GetItem(new AssetLocation(ItemTemporalSewingKit.KitCode));
        if (kit == null) return help;

        return help.Append(new WorldInteraction
        {
            ActionLangCode = "nf3ttapestryrepair:blockhelp-tapestry-repair-1",
            Itemstacks = [new ItemStack(kit)],
            MouseButton = EnumMouseButton.Right
        });
    }
}
