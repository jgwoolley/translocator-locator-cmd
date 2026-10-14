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
    private const string RepairItem = "flaxtwine";
    private const int RepairCost = 2;

    // BlockEntityTapestry exposes Rotten read-only, so the backing field is set via reflection.
    private static readonly FieldInfo? RottenField =
        typeof(BlockEntityTapestry).GetField("rotten", BindingFlags.Instance | BindingFlags.NonPublic);

    private static bool IsRepairItem(ItemSlot slot) =>
        !slot.Empty && slot.Itemstack.Collectible.Code.Path == RepairItem && slot.StackSize >= RepairCost;

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityTapestry;
        var slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (be is { Rotten: true } && RottenField != null && IsRepairItem(slot))
        {
            if (world.Side == EnumAppSide.Server)
            {
                RottenField.SetValue(be, false);
                if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative) slot.TakeOut(RepairCost);
                slot.MarkDirty();
                be.MarkDirty(true);
                world.PlaySoundAt(new AssetLocation("sounds/block/cloth"), blockSel.Position, 0.5, byPlayer);
            }

            return true;
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
    {
        var info = base.GetPlacedBlockInfo(world, pos, forPlayer);
        if (world.BlockAccessor.GetBlockEntity(pos) is not BlockEntityTapestry { Rotten: true }) return info;
        // The base info is trimmed, so it has no trailing newline to separate our line from it.
        var hint = Lang.Get("Hold flax twine and interact to repair.");
        return string.IsNullOrEmpty(info) ? hint : info.TrimEnd() + "\n" + hint;
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection,
        IPlayer forPlayer)
    {
        var help = base.GetPlacedBlockInteractionHelp(world, selection, forPlayer) ?? [];
        if (world.BlockAccessor.GetBlockEntity(selection.Position) is not BlockEntityTapestry { Rotten: true })
            return help;

        var twine = world.GetItem(new AssetLocation(RepairItem));
        if (twine == null) return help;

        return help.Append(new WorldInteraction
        {
            ActionLangCode = "nf3ttapestryrepair:blockhelp-tapestry-repair-1",
            Itemstacks = [new ItemStack(twine, RepairCost)],
            MouseButton = EnumMouseButton.Right
        });
    }
}
