namespace Nf3t.VintageStory.SchematicLocator;

using ProtoBuf;
using System.Collections.Generic;

// The packet sent from Server -> Client containing results
[ProtoContract]
public class SchematicSearchResults
{
    [ProtoMember(1)]
    public List<SchematicSearchResult> Results { get; set; } = new();
}

[ProtoContract]
public class SchematicSearchResult
{
    [ProtoMember(1)]
    public string AssetLocation { get; set; }
    [ProtoMember(2)]
    public string MatchedBlock { get; set; }
    [ProtoMember(3)]
    public int Count { get; set; }
}