using Newtonsoft.Json;

namespace Nf3t.VintageStory.Common;

public class SerializedSaveData
{
    [JsonProperty] public Dictionary<string, List<TranslocatorEntry>> TranslocatorsPerSavegame { get; } = new();

    [JsonProperty] public Dictionary<string, SimplePos> DefaultSpawnPositionPerSavegame { get; } = new();

    [JsonProperty] public Dictionary<string, TranslocatorPath> LastTranslocatorPathPerSavegame { get; } = new();

    [JsonProperty] public Dictionary<string, List<WayPoint>> WayPointsPerSavegame { get; } = new();
}