using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Nf3t.VintageStory.TranslocatorRepair;

public class BlockRepairableStaticTranslocator : BlockStaticTranslocator
{
    public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
    {
        if (Variant["state"] != "unrepairable") return base.GetPlacedBlockInfo(world, pos, forPlayer);
        return Lang.Get("Seems to be missing a cupronickel rod.");
    }

    private static int GetStackSize(IPlayer forPlayer)
    {
        var watchedAttributes = forPlayer.Entity.WatchedAttributes;
        var characterClass = watchedAttributes.GetString("characterClass", "");
        return characterClass == "clockmaker" ? 1 : 3;
    }

    public override bool OnBlockInteractStart(
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection blockSel)
    {
        if (Variant["state"] == "unrepairable")
        {
            var stackSize = GetStackSize(byPlayer);
            var activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (!activeHotbarSlot.Empty && activeHotbarSlot.Itemstack.Collectible.Code.Path == "rod-cupronickel" &&
                activeHotbarSlot.StackSize >= stackSize)
            {
                if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative) activeHotbarSlot.TakeOut(stackSize);

                world.PlaySoundAt(new AssetLocation("sounds/effect/latch"), blockSel.Position, -0.25, byPlayer,
                    range: 16f);
                var block = world.GetBlock(CodeWithVariant("state", "broken"));
                if (block != null)
                {
                    world.BlockAccessor.SetBlock(block.Id, blockSel.Position);
                    return true;
                }
            }
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(
        IWorldAccessor world,
        BlockSelection selection,
        IPlayer forPlayer)
    {
        if (Variant["state"] != "unrepairable") return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
        var stackSize = GetStackSize(forPlayer);

        return
        [
            new WorldInteraction
            {
                ActionLangCode = "blockhelp-translocator-repair-1",
                Itemstacks =
                [
                    new ItemStack(world.GetItem(new AssetLocation("rod-cupronickel")), stackSize)
                ],
                MouseButton = EnumMouseButton.Right
            }
        ];
    }
}