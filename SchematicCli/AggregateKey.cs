using Vintagestory.API.Common;

namespace Nf3t.VintageStory.SchematicCli;

public record AggregateKey(AssetLocation? AssetLocation, string? TreeKey, string? TreeValue);