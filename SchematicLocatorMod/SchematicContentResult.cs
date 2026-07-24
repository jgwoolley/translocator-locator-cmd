using ProtoBuf;

namespace Nf3t.VintageStory.SchematicLocator;

[ProtoContract]
public class SchematicContentResult
{
    [ProtoMember(1)] public string AssetLocation { get; set; } = "";
    [ProtoMember(2)] public string Content { get; set; } = "";
}