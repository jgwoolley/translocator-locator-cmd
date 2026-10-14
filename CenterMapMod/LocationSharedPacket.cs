using ProtoBuf;

namespace Nf3t.VintageStory.Sponge;

/// <summary>Server to clients: a player shared a location. The player name is set by the server.</summary>
[ProtoContract]
public class LocationSharedPacket
{
    [ProtoMember(1)] public string PlayerName { get; set; } = "";
    [ProtoMember(2)] public int X { get; set; }
    [ProtoMember(3)] public int Y { get; set; }
    [ProtoMember(4)] public int Z { get; set; }
    [ProtoMember(5)] public string Name { get; set; } = "";
}
