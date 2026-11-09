using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Nf3t.VintageStory.Statue;

/// <summary>What a statue block is made of: a rock (<c>statue-*</c> blocks) or a metal (<c>statuemetal-*</c> blocks).</summary>
public static class StatueMaterial
{
    /// <summary>
    /// Whether the metal is shiny. The game makes its metal blocks shine by flagging their vertices as reflective, except
    /// for iron and lead, and the statues follow suit.
    /// </summary>
    public static bool IsShiny(string? metal) => metal != null && metal is not ("iron" or "lead");

    /// <summary>
    /// A block's display name with its material filled in. Lang files cannot see the variant, so the material's own
    /// name (the game's rock names, or this mod's metal names) is passed in as {0}.
    /// </summary>
    public static string NameOf(Block block, string nameKey) =>
        Lang.Get(nameKey, block.Variant["metal"] is { } metal
            ? Lang.Get("nf3tstatue:metal-" + metal)
            : Lang.Get("rock-" + block.Variant["rock"]));
}
