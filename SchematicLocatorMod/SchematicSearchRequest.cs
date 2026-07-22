#nullable enable
namespace Nf3t.VintageStory.SchematicLocator;

using ProtoBuf;

[ProtoContract]
public class SchematicSearchRequest
{
    [ProtoMember(1)] public string SearchBlockPrefix { get; set; } = "";
    [ProtoMember(2)]
    public string TreeKey { get; set; } = "";
    [ProtoMember(3)]
    public string TreeValue { get; set; } = "";
    [ProtoMember(4)]
    public string Domain { get; set; } = "";
}