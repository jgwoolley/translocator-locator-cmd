using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Nf3t.VintageStory.Statue;

/// <summary>The design of a finished statue as it is kept on its item: who, which pose, what is worn and held.</summary>
public record StatueDesign(string PlayerName, string Emote, int Frame, StatueAppearance Appearance)
{
    public string Type => Appearance.Type;

    /// <summary>The design on a statue item, or null for anything else (a blank, a stack without a design).</summary>
    public static StatueDesign? FromStack(ItemStack? stack, IWorldAccessor world)
    {
        if (stack?.Collectible is not BlockStatue || stack.Attributes[BlockEntityStatue.AttributesKey] is not ITreeAttribute tree)
            return null;

        var appearance = StatueAppearance.Read(tree, world);
        var emote = tree.GetString("emote", "");

        // A special statue has no pose; only a player statue without one has no design.
        if (emote == "" && appearance.Type == StatueTypes.Player) return null;

        return new StatueDesign(tree.GetString("playerName", ""), emote, tree.GetInt("frame", -1), appearance);
    }
}
