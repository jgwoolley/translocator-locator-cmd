using ProtoBuf;

namespace Nf3t.VintageStory.Sponge;

/// <summary>Client to server: "share this location with everyone".</summary>
[ProtoContract]
public class ShareLocationPacket
{
    [ProtoMember(1)] public int X { get; set; }
    [ProtoMember(2)] public int Y { get; set; }
    [ProtoMember(3)] public int Z { get; set; }
    [ProtoMember(4)] public string Name { get; set; } = "";
}
