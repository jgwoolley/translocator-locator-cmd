using System;
using System.Linq;

namespace Nf3t.VintageStory.Statue;

/// <summary>
/// What a statue shows. The default is a <see cref="Player"/>; the others are special statues with a fixed shape (a
/// robot, for now) that are carved, copied, packed in clay and cast exactly like a player statue, just with fewer options.
/// A new special statue only needs an entry in <see cref="Specials"/>, a shape and a lang name.
/// </summary>
public static class StatueTypes
{
    public const string Player = "player";

    /// <summary>
    /// A special statue: its code, the shape it is drawn from (with its front on the south face, +Z, and with its own
    /// thin pedestal slab as an element named "Pedestal", which is left out) and how tall the figure is above that slab,
    /// in blocks, which is how it is scaled to fill a block.
    /// </summary>
    public record Special(string Code, string ShapePath, float FigureHeight);

    public static readonly Special[] Specials = [new("robot", "nf3tstatue:shapes/block/robot.json", 30.8f / 16)];

    /// <summary>The element of a special shape that is its own pedestal; the statue's pedestal replaces it.</summary>
    public const string PedestalElement = "Pedestal";

    /// <summary>Every type that can be picked, the player first.</summary>
    public static string[] All => [Player, .. Specials.Select(s => s.Code)];

    public static bool IsKnown(string code) => All.Contains(code);

    public static Special? SpecialOf(string code) => Specials.FirstOrDefault(s => s.Code == code);

    public static string LangKey(string code) => "nf3tstatue:type-" + code;

    /// <summary>
    /// How much a special figure is scaled: it fills its block as far as it can, which is the height of the block (a bust
    /// is drawn like a small statue) for a small statue, and the 3x3x4 volume of a large statue, in which a figure is
    /// limited by its height. Room is left for the pedestal.
    /// </summary>
    public static float ScaleFor(Special special, string size)
    {
        var room = size == "large" ? 3.75f : 0.875f;
        return room / special.FigureHeight;
    }
}
