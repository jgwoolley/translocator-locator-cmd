using System.Linq;

namespace Nf3t.VintageStory.Statue;

/// <summary>
/// The tack an elk can wear on a statue. These are the game's own elk wearables; the statue only needs their names, so
/// the player does not have to own the items.
/// </summary>
public static class StatueTack
{
    public enum Piece
    {
        Bridle = 0,
        Face = 1,
        Saddle = 2,
        Bags = 3,
    }

    public const int PieceCount = 4;

    /// <summary>The empty string means the elk wears nothing in that place.</summary>
    public const string None = "";

    private static readonly string[][] Types =
    [
        [None, "bridle1", "bridle2"],
        [None, .. Enumerable.Range(1, 16).Select(i => "face" + i)],
        [None, "saddle1"],
        [None, "saddlebags1", "saddlebags2"],
    ];

    public static string[] Choices(Piece piece) => Types[(int)piece];

    public static bool IsValid(Piece piece, string type) => Types[(int)piece].Contains(type);

    public static string[] Empty() => Enumerable.Repeat(None, PieceCount).ToArray();

    /// <summary>The shape the game attaches to an elk for this piece of tack.</summary>
    public static string ShapePath(Piece piece, string type) => piece == Piece.Face
        ? $"game:shapes/item/wearable/hooved/face/{type}-elk.json"
        : $"game:shapes/item/wearable/hooved/{type}-elk.json";

    /// <summary>The key into the lang file for the name of a type (faces share one key and are numbered).</summary>
    public static string LangKey(Piece piece, string type) => type == None ? "nf3tstatue:tack-none"
        : piece == Piece.Face ? "nf3tstatue:tack-face" : "nf3tstatue:tack-" + type;

    /// <summary>
    /// Which piece of tack an item is, from its code (for example <c>hoovedwearables-middleback-saddle1</c>), or null when it
    /// is not elk tack the statue knows.
    /// </summary>
    public static (Piece Piece, string Type)? FromWearableCode(string path)
    {
        var parts = path.Split('-');
        if (parts.Length != 3 || parts[0] != "hoovedwearables") return null;

        Piece? piece = parts[1] switch
        {
            "head" => Piece.Bridle,
            "face" => Piece.Face,
            "middleback" => Piece.Saddle,
            "lowerbackside" => Piece.Bags,
            _ => null,
        };
        return piece is { } known && IsValid(known, parts[2]) ? (known, parts[2]) : null;
    }
}
