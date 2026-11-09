using Vintagestory.API.Common;

namespace Nf3t.VintageStory.Statue;

public class StatueModSystem : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.RegisterBlockClass("BlockStatue", typeof(BlockStatue));
        api.RegisterBlockClass("BlockStatueBlank", typeof(BlockStatueBlank));
        api.RegisterBlockClass("BlockStatueMold", typeof(BlockStatueMold));
        api.RegisterBlockEntityClass("BlockEntityStatue", typeof(BlockEntityStatue));
        api.RegisterBlockEntityClass("BlockEntityStatueMold", typeof(BlockEntityStatueMold));
    }

    public override void Dispose()
    {
        base.Dispose();
        StatueMesh.ClearCache();
    }
}
