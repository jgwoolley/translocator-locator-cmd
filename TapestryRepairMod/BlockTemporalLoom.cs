#nullable enable

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Nf3t.VintageStory.TapestryRepair;

/// <summary>
/// The temporal loom: a Jonas device (a 1x2 multiblock) powered by temporal gears that weaves flax twine into temporal
/// fiber. Right-click with a temporal gear to power it, with flax twine to feed it, and with an empty hand to take the fiber.
/// The state lives in <see cref="BlockEntityTemporalLoom"/>; the upper half of the multiblock forwards here.
/// </summary>
public class BlockTemporalLoom : Block
{
    public const string GearCode = "game:gear-temporal";
    public const string FlaxCode = "game:flaxtwine";

    /// <summary>The gear that powers this loom: a temporal gear here, a rusty gear for the unraveling loom.</summary>
    protected virtual string PowerCode => GearCode;

    private static bool Is(ItemSlot slot, string code) => !slot.Empty && slot.Itemstack.Collectible.Code.ToString() == code;

    private void Error(IWorldAccessor world, string key) =>
        (world.Api as ICoreClientAPI)?.TriggerIngameError(this, key, Lang.Get(key));

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntityTemporalLoom loom)
            return base.OnBlockInteractStart(world, byPlayer, blockSel);

        if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            return base.OnBlockInteractStart(world, byPlayer, blockSel);

        var slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        var creative = byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative;
        var server = world.Side == EnumAppSide.Server;

        if (Is(slot, PowerCode))
        {
            if (loom.Charge + BlockEntityTemporalLoom.ChargePerGear > BlockEntityTemporalLoom.MaxCharge)
            {
                Error(world, "nf3ttapestryrepair:ingameerror-loom-full-power");
                return true;
            }

            if (!server) return true;
            loom.AddGear();
            if (!creative) slot.TakeOut(1);
            slot.MarkDirty();
            world.PlaySoundAt(new AssetLocation("sounds/effect/gears"), blockSel.Position, 0.5, byPlayer, true, 16, 0.6f);
            return true;
        }

        if (Is(slot, FlaxCode))
        {
            if (loom.Flax >= BlockEntityTemporalLoom.MaxFlax)
            {
                Error(world, "nf3ttapestryrepair:ingameerror-loom-full-flax");
                return true;
            }

            if (!server) return true;
            var added = loom.AddFlax(slot.StackSize);
            if (!creative) slot.TakeOut(added);
            slot.MarkDirty();
            world.PlaySoundAt(new AssetLocation("sounds/block/cloth"), blockSel.Position, 0.5, byPlayer);
            return true;
        }

        if (slot.Empty && loom.Fiber > 0)
        {
            if (!server) return true;
            var fiber = loom.TakeFiber();
            if (fiber == null) return true;
            if (!byPlayer.InventoryManager.TryGiveItemstack(fiber))
                world.SpawnItemEntity(fiber, byPlayer.Entity.Pos.XYZ.Add(0, 0.5, 0));
            world.PlaySoundAt(new AssetLocation("sounds/block/cloth"), blockSel.Position, 0.5, byPlayer);
            return true;
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection,
        IPlayer forPlayer)
    {
        var help = base.GetPlacedBlockInteractionHelp(world, selection, forPlayer) ?? [];
        var gear = world.GetItem(new AssetLocation(PowerCode));
        var flax = world.GetItem(new AssetLocation(FlaxCode));
        if (gear == null || flax == null) return help;

        return help.Append(new WorldInteraction
            {
                ActionLangCode = "nf3ttapestryrepair:blockhelp-temporalloom-gear",
                Itemstacks = [new ItemStack(gear)],
                MouseButton = EnumMouseButton.Right
            })
            .Append(new WorldInteraction
            {
                ActionLangCode = "nf3ttapestryrepair:blockhelp-temporalloom-flax",
                Itemstacks = [new ItemStack(flax)],
                MouseButton = EnumMouseButton.Right
            })
            .Append(new WorldInteraction
            {
                ActionLangCode = "nf3ttapestryrepair:blockhelp-temporalloom-fiber",
                MouseButton = EnumMouseButton.Right
            });
    }
}
