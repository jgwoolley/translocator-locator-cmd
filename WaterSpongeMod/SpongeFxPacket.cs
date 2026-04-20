using ProtoBuf;

namespace Nf3t.VintageStory.Sponge;

[ProtoContract]
public class SpongeFxPacket
{
    [ProtoMember(1)] public int Dimension { get; set; }

    [ProtoMember(2)] public double X { get; set; }
    [ProtoMember(3)] public double Y { get; set; }
    [ProtoMember(4)] public double Z { get; set; }

    // If you keep normals:
    [ProtoMember(5)] public float Nx { get; set; }
    [ProtoMember(6)] public float Ny { get; set; }
    [ProtoMember(7)] public float Nz { get; set; }
}