#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Nf3t.VintageStory.BlockColor;

public class BlockColorModSystem : ModSystem
{
    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Client;

    public override void StartClientSide(ICoreClientAPI api)
    {
        api.ChatCommands.Create("blockcolors")
            .WithDescription(
                "Writes an average color for every block to ModData/Nf3tBlockColors.json. Optional wildcard filter on the block code, e.g. game:rock-*")
            .WithArgs(api.ChatCommands.Parsers.OptionalWord("filter"))
            .HandleWith(args => Generate(api, args[0] as string));
    }

    private static TextCommandResult Generate(ICoreClientAPI api, string? filter)
    {
        var colors = new SortedDictionary<string, BlockColorEntry>(StringComparer.Ordinal);
        var missing = 0;

        foreach (var block in api.World.Blocks)
        {
            if (block?.Code == null || block.Id == 0) continue;
            if (!string.IsNullOrEmpty(filter) && !WildcardUtil.Match(filter, block.Code.ToString())) continue;

            // The texture the game itself uses for this block's color on the world map.
            // Blocks without one (invisible or special-rendered) are white.
            if (block.TextureSubIdForBlockColor < 0)
            {
                missing++;
                colors[block.Code.ToString()] = new BlockColorEntry(ColorUtil.Int2Hex(ColorUtil.WhiteArgb), 255);
                continue;
            }

            var argb = api.BlockTextureAtlas.GetAverageColor(block.TextureSubIdForBlockColor);
            colors[block.Code.ToString()] = new BlockColorEntry(ColorUtil.Int2Hex(argb), ColorUtil.ColorA(argb));
        }

        var path = Path.Combine(GamePaths.DataPath, "ModData", "Nf3tBlockColors.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonConvert.SerializeObject(colors, Formatting.Indented));

        return TextCommandResult.Success(
            $"Wrote {colors.Count} block color(s) to {path}. {missing} of them have no color texture and were set to white.");
    }
}

public record BlockColorEntry([property: JsonProperty("hex")] string Hex, [property: JsonProperty("alpha")] int Alpha);
