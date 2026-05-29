#nullable enable

using Vintagestory.API.Common;

namespace Nf3t.VintageStory.TranslocatorRepair;

public class TranslocatorRepairModSystem : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.RegisterBlockClass("BlockRepairableStaticTranslocator", typeof(BlockRepairableStaticTranslocator));
    }
}