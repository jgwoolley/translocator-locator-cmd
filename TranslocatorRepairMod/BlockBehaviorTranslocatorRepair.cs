#nullable enable

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.TranslocatorRepair;

/// <summary>
/// Lets an "unrepairable" static translocator be repaired with cupronickel rods.
/// Implemented as a behavior rather than a block class, so it is added alongside whatever class
/// (and behaviors) other mods give the translocator instead of replacing them.
/// </summary>
public class BlockBehaviorTranslocatorRepair(Block block) : BlockBehavior(block)
{
    private const string RepairItemPath = "rod-cupronickel";

    private bool IsUnrepairable => block.Variant["state"] == "unrepairable";

    private static int GetStackSize(IPlayer forPlayer)
    {
        var watchedAttributes = forPlayer.Entity.WatchedAttributes;
        var characterClass = watchedAttributes.GetString("characterClass", "");
        return characterClass == "clockmaker" ? 1 : 3;
    }

    public override bool OnBlockInteractStart(
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection blockSel,
        ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        if (!IsUnrepairable) return false;

        var stackSize = GetStackSize(byPlayer);
        var activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if (activeHotbarSlot.Empty || activeHotbarSlot.Itemstack.Collectible.Code.Path != RepairItemPath ||
            activeHotbarSlot.StackSize < stackSize)
            return false;

        var repaired = world.GetBlock(block.CodeWithVariant("state", "broken"));
        if (repaired == null) return false;

        if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative) activeHotbarSlot.TakeOut(stackSize);

        world.PlaySoundAt(new AssetLocation("sounds/effect/latch"), blockSel.Position, -0.25, byPlayer, range: 16f);
        world.BlockAccessor.SetBlock(repaired.Id, blockSel.Position);

        handling = EnumHandling.PreventDefault;
        return true;
    }

    public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
    {
        return IsUnrepairable ? "\n" + Lang.Get("Seems to be missing a cupronickel rod.") : "";
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(
        IWorldAccessor world,
        BlockSelection selection,
        IPlayer forPlayer,
        ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        if (!IsUnrepairable) return [];

        var rod = world.GetItem(new AssetLocation(RepairItemPath));
        if (rod == null) return [];

        return
        [
            new WorldInteraction
            {
                ActionLangCode = "blockhelp-translocator-repair-1",
                Itemstacks = [new ItemStack(rod, GetStackSize(forPlayer))],
                MouseButton = EnumMouseButton.Right
            }
        ];
    }
}
