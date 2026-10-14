#nullable enable

using Vintagestory.API.Common;

namespace Nf3t.VintageStory.TapestryRepair;

public class TapestryRepairModSystem : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.RegisterBlockClass("BlockRepairableTapestry", typeof(BlockRepairableTapestry));
    }
}
