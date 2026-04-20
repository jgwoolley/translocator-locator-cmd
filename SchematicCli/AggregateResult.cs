using Vintagestory.API.Common;

namespace Nf3t.VintageStory.SchematicCli;


public record struct AggregateResult(AssetLocation? AssetLocation, string? TreeKey, string? TreeValue, int Count);