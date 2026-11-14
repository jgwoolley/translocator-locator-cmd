#nullable enable

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Nf3t.VintageStory.EntropyRift;

/// <summary>
/// The Storm Siphon: a Jonas device (1x2 multiblock, lightning rod design). Right-click it with a temporal gear to set the
/// gear in its cradle; it then tells you when the next temporal storm comes. During a storm, under open sky, it draws the
/// storm into the gear until the gear becomes an entropy gear. Right-click with an empty hand to take the gear out.
/// </summary>
public class BlockStormSiphon : Block
{
    public const string TemporalGearCode = "game:gear-temporal";
    public const string EntropyGearCode = "nf3tentropyrift:entropygear";

    private static bool Is(ItemSlot slot, string code) => !slot.Empty && slot.Itemstack.Collectible.Code.ToString() == code;

    private void Error(IWorldAccessor world, string key) =>
        (world.Api as ICoreClientAPI)?.TriggerIngameError(this, key, Lang.Get(key));

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntityStormSiphon siphon)
            return base.OnBlockInteractStart(world, byPlayer, blockSel);

        if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
            return base.OnBlockInteractStart(world, byPlayer, blockSel);

        var slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        var server = world.Side == EnumAppSide.Server;

        if (Is(slot, TemporalGearCode))
        {
            if (siphon.State != "empty")
            {
                Error(world, "nf3tentropyrift:ingameerror-siphon-full");
                return true;
            }

            if (!server) return true;
            siphon.InsertTemporalGear();
            if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative) slot.TakeOut(1);
            slot.MarkDirty();
            world.PlaySoundAt(new AssetLocation("sounds/effect/gears"), blockSel.Position, 0.5, byPlayer, true, 16, 0.6f);
            Tell(byPlayer, siphon);
            return true;
        }

        if (slot.Empty && siphon.State != "empty")
        {
            if (!server) return true;
            var gear = siphon.TakeGear();
            if (gear == null) return true;
            if (!byPlayer.InventoryManager.TryGiveItemstack(gear))
                world.SpawnItemEntity(gear, byPlayer.Entity.Pos.XYZ.Add(0, 0.5, 0));
            world.PlaySoundAt(new AssetLocation("sounds/player/collect"), blockSel.Position, 0.5, byPlayer);
            return true;
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    /// <summary>Chat message after setting a gear: when the next storm comes, and whether the rod can reach the sky.</summary>
    private static void Tell(IPlayer byPlayer, BlockEntityStormSiphon siphon)
    {
        if (byPlayer is not IServerPlayer player) return;
        var text = Lang.GetL(player.LanguageCode, "nf3tentropyrift:siphon-msg-set") + " " + StormForecast.Describe(siphon.Api, player.LanguageCode);
        if (!siphon.SkyOpen()) text += " " + Lang.GetL(player.LanguageCode, "nf3tentropyrift:siphon-info-nosky");
        player.SendMessage(GlobalConstants.GeneralChatGroup, text, EnumChatType.Notification);
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection,
        IPlayer forPlayer)
    {
        var help = base.GetPlacedBlockInteractionHelp(world, selection, forPlayer) ?? [];
        var state = Variant["state"];
        if (state == "empty")
        {
            var gear = world.GetItem(new AssetLocation(TemporalGearCode));
            if (gear == null) return help;
            return help.Append(new WorldInteraction
            {
                ActionLangCode = "nf3tentropyrift:blockhelp-siphon-set",
                Itemstacks = [new ItemStack(gear)],
                MouseButton = EnumMouseButton.Right
            });
        }

        return help.Append(new WorldInteraction
        {
            ActionLangCode = "nf3tentropyrift:blockhelp-siphon-take",
            MouseButton = EnumMouseButton.Right
        });
    }
}
