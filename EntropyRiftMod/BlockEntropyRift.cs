#nullable enable

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Nf3t.VintageStory.EntropyRift;

/// <summary>
/// The Entropy Rift (1x2 multiblock, lightning rod design). Right-click it with an entropy gear to power it. The gear cannot
/// be taken out again: it is spent pulse by pulse drawing rust creatures out. The state lives in <see cref="BlockEntityEntropyRift"/>.
/// </summary>
public class BlockEntropyRift : Block
{
    private static bool Is(ItemSlot slot, string code) => !slot.Empty && slot.Itemstack.Collectible.Code.ToString() == code;

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntityEntropyRift rift)
            return base.OnBlockInteractStart(world, byPlayer, blockSel);

        if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
            return base.OnBlockInteractStart(world, byPlayer, blockSel);

        var slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        var server = world.Side == EnumAppSide.Server;

        if (Is(slot, BlockStormSiphon.EntropyGearCode))
        {
            if (rift.Powered)
            {
                const string key = "nf3tentropyrift:ingameerror-rift-full";
                (world.Api as ICoreClientAPI)?.TriggerIngameError(this, key, Lang.Get(key));
                return true;
            }

            if (!server) return true;
            rift.InsertEntropyGear();
            if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative) slot.TakeOut(1);
            slot.MarkDirty();
            return true;
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection,
        IPlayer forPlayer)
    {
        var help = base.GetPlacedBlockInteractionHelp(world, selection, forPlayer) ?? [];
        if (Variant["state"] == "powered") return help;   // the gear stays in until it is spent

        var gear = world.GetItem(new AssetLocation(BlockStormSiphon.EntropyGearCode));
        if (gear == null) return help;
        return help.Append(new WorldInteraction
        {
            ActionLangCode = "nf3tentropyrift:blockhelp-rift-power",
            Itemstacks = [new ItemStack(gear)],
            MouseButton = EnumMouseButton.Right
        });
    }
}
