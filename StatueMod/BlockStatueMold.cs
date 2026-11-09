using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Nf3t.VintageStory.Statue;

/// <summary>
/// A clay mold made around a stone statue. It is the game's tool mold (metal is poured in, and the mold breaks when
/// filled), but it remembers the statue it was made around, and the cast statue gets the same design.
/// </summary>
public class BlockStatueMold : BlockToolMold
{
    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
        var drops = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityStatueMold { Design: { } design })
        {
            // Both the cast statue and the mold itself (when it is picked up unfilled) keep the design.
            foreach (var stack in drops)
            {
                if (stack.Collectible is BlockStatue or BlockStatueMold) stack.Attributes[BlockEntityStatue.AttributesKey] = design.Clone();
            }
        }

        return drops;
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        var stack = base.OnPickBlock(world, pos);
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityStatueMold { Design: { } design })
            stack.Attributes[BlockEntityStatue.AttributesKey] = design.Clone();
        return stack;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        dsc.AppendLine(Lang.Get("nf3tstatue:info-mold-use"));
        var design = BlockEntityStatueMold.DesignOf(inSlot.Itemstack);
        if (design == null)
        {
            dsc.AppendLine(Lang.Get("nf3tstatue:info-mold-blank"));
            return;
        }

        var type = design.GetString("statueType", StatueTypes.Player);
        dsc.AppendLine(type != StatueTypes.Player && StatueTypes.IsKnown(type)
            ? Lang.Get("nf3tstatue:info-mold-special", Lang.Get(StatueTypes.LangKey(type)))
            : Lang.Get("nf3tstatue:info-statue", design.GetString("playerName", "?"), Lang.Get("nf3tstatue:emote-" + design.GetString("emote", "none"))));
    }
}

/// <summary>The block entity of a statue mold: the tool mold, plus the design of the statue it was made around.</summary>
public class BlockEntityStatueMold : BlockEntityToolMold
{
    /// <summary>The design, in the same form a statue item keeps it (see <see cref="BlockEntityStatue.WriteToStack"/>).</summary>
    public ITreeAttribute? Design;

    public static ITreeAttribute? DesignOf(ItemStack? stack) => stack?.Attributes[BlockEntityStatue.AttributesKey] as ITreeAttribute;

    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);
        Design = DesignOf(byItemStack)?.Clone() as ITreeAttribute;
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        if (Design != null) tree[BlockEntityStatue.AttributesKey] = Design.Clone();
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
    {
        base.FromTreeAttributes(tree, worldForResolve);
        Design = tree[BlockEntityStatue.AttributesKey] as ITreeAttribute;
    }
}
