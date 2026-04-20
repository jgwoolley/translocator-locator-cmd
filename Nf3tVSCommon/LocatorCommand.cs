using Newtonsoft.Json;

namespace Nf3t.VintageStory.Common;

public class LocatorCommand
{
    public LocatorCommand()
    {
    }

    [JsonConstructor]
    public LocatorCommand(string name, string description, bool closestOnly, string keyword)
    {
        Name = name;
        Description = description;
        ClosestOnly = closestOnly;
        Keyword = keyword;
    }

    [JsonProperty] public string Name { get; set; } = "";
    [JsonProperty] public string Description { get; set; } = "";
    [JsonProperty] public bool ClosestOnly { get; set; } = true;
    [JsonProperty] public string Keyword { get; set; } = "";
}