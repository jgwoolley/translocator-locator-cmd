using Vintagestory.API.Common;

namespace Nf3t.VintageStory.SchematicCli;

public record struct Result(uint Index, int BlockId, AssetLocation? AssetLocation, string? TreeKey, string? TreeValue);