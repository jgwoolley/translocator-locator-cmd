using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Nf3t.VintageStory.Statue;

/// <summary>
/// A plain block of stone that becomes a statue. Right-clicking it with a chisel in the hand and a hammer in the
/// off hand opens the design dialog; committing the design turns it into the finished statue for good.
/// </summary>
public class BlockStatueBlank : BlockStatue
{
    /// <summary>How much the chisel and the hammer wear down when a statue is carved.</summary>
    public const int ToolWear = 25;

    protected override string NameKey => "nf3tstatue:name-blank-" + Variant["size"];

    /// <summary>The finished statue this block turns into: the same size and rock.</summary>
    public Block? FinishedBlock(IWorldAccessor world) =>
        world.GetBlock(new AssetLocation(Code.Domain, $"statue-{Variant["size"]}-{Variant["rock"]}"));

    /// <summary>A chisel in the active hand and a hammer in the off hand, the way the game's own chiseling works.</summary>
    public static bool HasTools(IPlayer player) =>
        player.InventoryManager is { ActiveTool: EnumTool.Chisel, OffhandTool: EnumTool.Hammer };

    /// <summary>Wears down both tools (the game destroys one that has no durability left).</summary>
    public static void WearTools(IWorldAccessor world, IPlayer player)
    {
        if (player.WorldData.CurrentGameMode == EnumGameMode.Creative) return;
        var inventory = player.InventoryManager;
        foreach (var slot in new[] { inventory.ActiveHotbarSlot, inventory.OffhandHotbarSlot })
        {
            slot.Itemstack?.Collectible.DamageItem(world, player.Entity, slot, ToolWear);
        }
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (world.Side != EnumAppSide.Client) return true;
        if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            return base.OnBlockInteractStart(world, byPlayer, blockSel);

        if (!HasTools(byPlayer))
        {
            ((ICoreClientAPI)world.Api).TriggerIngameError(this, "needtools", Lang.Get("nf3tstatue:need-tools"));
            return true;
        }

        (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityStatue)?.OpenDialog();
        return true;
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection,
        IPlayer forPlayer)
    {
        return
        [
            new WorldInteraction
            {
                ActionLangCode = "nf3tstatue:help-carve",
                MouseButton = EnumMouseButton.Right,
            },
        ];
    }
}
