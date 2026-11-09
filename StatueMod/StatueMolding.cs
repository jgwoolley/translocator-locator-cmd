using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.Statue;

/// <summary>Making a mold: fire clay is packed around a finished stone statue, then the mold is taken off it.</summary>
public static class StatueMolding
{
    /// <summary>The fire clay a statue of this size takes to be covered completely.</summary>
    public static int ClayNeeded(string size) => size switch
    {
        "bust" => 5,
        "large" => 16,
        _ => 8,
    };

    public static bool IsFireClay(ItemStack? stack) =>
        stack?.Collectible.Code is { Domain: "game", Path: "clay-fire" };

    /// <summary>
    /// The packing and taking of a mold, shared by every statue that can be a mold bed. Packs one piece of the fire clay in
    /// the player's hand (raising <paramref name="level"/>), or, once the statue is buried, hands over the mold and clears
    /// the clay. Returns whether the interaction was this one; the caller saves and redraws when <paramref name="level"/> changed.
    /// </summary>
    public static bool Interact(ICoreAPI api, IPlayer player, BlockPos pos, Block bed, ref int level, int needed,
        Block? mold, Action<ItemStack>? writeDesign)
    {
        var slot = player.InventoryManager.ActiveHotbarSlot;
        var packing = IsFireClay(slot.Itemstack) && level < needed;
        var taking = slot.Empty && level >= needed;
        if (!packing && !taking) return false;
        // A claimed statue is only a mold bed for those who may build there.
        if (!api.World.Claims.TryAccess(player, pos, EnumBlockAccessFlags.BuildOrBreak)) return false;
        if (api.Side != EnumAppSide.Server) return true;

        if (packing)
        {
            if (player.WorldData.CurrentGameMode != EnumGameMode.Creative) slot.TakeOut(1);
            slot.MarkDirty();
            level++;
        }
        else
        {
            if (mold == null) return false;

            var stack = new ItemStack(mold);
            writeDesign?.Invoke(stack);
            if (!player.InventoryManager.TryGiveItemstack(stack)) api.World.SpawnItemEntity(stack, pos.ToVec3d().Add(0.5, 0.5, 0.5));
            level = 0;
        }

        api.World.PlaySoundAt(bed.Sounds.Place, pos, 0, player);
        return true;
    }
}
