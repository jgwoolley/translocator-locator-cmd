using ProtoBuf;

namespace Nf3t.VintageStory.SchematicLocator;

[ProtoContract]
public class SchematicContentRequest
{
    [ProtoMember(1)] public string AssetLocation { get; set; } = "";

}