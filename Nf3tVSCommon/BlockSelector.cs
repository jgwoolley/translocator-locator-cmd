using Newtonsoft.Json;

namespace Nf3t.VintageStory.Common;

public class BlockSelector
{
    public BlockSelector()
    {
    }

    [JsonConstructor]
    public BlockSelector(string startsWith, string icon, string color, string[] keywords)
    {
        StartsWith = startsWith;
        Color = color;
        Icon = icon;
        Keywords = keywords;
    }

    [JsonProperty] public string StartsWith { get; set; } = "";
    [JsonProperty] public string Color { get; set; } = "";
    [JsonProperty] public string Icon { get; set; } = "";
    [JsonProperty] public string[] Keywords { get; set; } = new string[] { };
}