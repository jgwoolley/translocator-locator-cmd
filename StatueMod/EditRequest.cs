using System;
using System.IO;

namespace Nf3t.VintageStory.Statue;

/// <summary>
/// Everything the dialog sends when the player commits a statue. Hands are hotbar slot numbers (or one of the
/// <see cref="StatueAppearance"/> hand codes), never items, so the server decides what is copied.
/// </summary>
public record EditRequest(string PlayerName, string Emote, int Frame, bool Look, bool Clothing, bool Armor,
    int RightHand, int LeftHand, float AngleY, string Mount, string ElkPose, int ElkFrame, string[] Tack, int CopySlot, int Antlers = StatueAppearance.DefaultAntlers,
    string StatueType = StatueTypes.Player, bool NoPedestal = false)
{
    /// <summary>No statue is copied.</summary>
    public const int NoCopy = -1;

    public byte[] Encode()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(PlayerName);
            writer.Write(Emote);
            writer.Write(Frame);
            writer.Write(Look);
            writer.Write(Clothing);
            writer.Write(Armor);
            writer.Write(RightHand);
            writer.Write(LeftHand);
            writer.Write(AngleY);
            writer.Write(Mount);
            writer.Write(ElkPose);
            writer.Write(ElkFrame);
            writer.Write(CopySlot);
            writer.Write(Antlers);
            writer.Write(StatueType);
            writer.Write(NoPedestal);
            writer.Write(Tack.Length);
            foreach (var piece in Tack) writer.Write(piece);
        }

        return stream.ToArray();
    }

    /// <summary>Reads a packet, or returns null when it is malformed (a client can send anything).</summary>
    public static EditRequest? Decode(byte[] data)
    {
        try
        {
            using var reader = new BinaryReader(new MemoryStream(data));
            var playerName = reader.ReadString().Trim();
            var emote = reader.ReadString();
            var frame = reader.ReadInt32();
            var look = reader.ReadBoolean();
            var clothing = reader.ReadBoolean();
            var armor = reader.ReadBoolean();
            var rightHand = reader.ReadInt32();
            var leftHand = reader.ReadInt32();
            var angleY = reader.ReadSingle();
            var mount = reader.ReadString();
            var elkPose = reader.ReadString();
            var elkFrame = reader.ReadInt32();
            var copySlot = reader.ReadInt32();
            var antlers = reader.ReadInt32();
            var statueType = reader.ReadString();
            var noPedestal = reader.ReadBoolean();
            var count = reader.ReadInt32();
            if (count is < 0 or > StatueTack.PieceCount) return null;

            var tack = StatueTack.Empty();
            for (var i = 0; i < count; i++) tack[i] = reader.ReadString();

            return new EditRequest(playerName, emote, frame, look, clothing, armor, rightHand, leftHand, angleY, mount,
                elkPose, elkFrame, tack, copySlot, antlers, statueType, noPedestal);
        }
        catch (Exception e) when (e is IOException or ArgumentException or FormatException)
        {
            return null;
        }
    }

    /// <summary>Whether the mount and every piece of tack are ones the mod knows (a bust cannot ride, so it has none).</summary>
    public bool HasValidMount(bool bust)
    {
        if (!StatueAppearance.IsValidMount(Mount, bust) || Antlers is < 0 or > StatueAppearance.MaxAntlers) return false;

        for (var i = 0; i < StatueTack.PieceCount; i++)
        {
            if (!StatueTack.IsValid((StatueTack.Piece)i, Tack[i])) return false;
        }

        return true;
    }
}
