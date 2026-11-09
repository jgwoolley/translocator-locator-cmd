#nullable enable

using Vintagestory.API.Common;

namespace Nf3t.VintageStory.TapestryRepair;

public class TapestryRepairModSystem : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.RegisterBlockClass("BlockRepairableTapestry", typeof(BlockRepairableTapestry));
        api.RegisterBlockClass("BlockTemporalLoom", typeof(BlockTemporalLoom));
        api.RegisterBlockClass("BlockDecayLoom", typeof(BlockDecayLoom));
        api.RegisterBlockEntityClass("TemporalLoom", typeof(BlockEntityTemporalLoom));
        api.RegisterBlockEntityClass("DecayLoom", typeof(BlockEntityDecayLoom));
        api.RegisterItemClass("ItemTemporalDecayKit", typeof(ItemTemporalDecayKit));
        api.RegisterItemClass("ItemTemporalSewingKit", typeof(ItemTemporalSewingKit));
    }
}
