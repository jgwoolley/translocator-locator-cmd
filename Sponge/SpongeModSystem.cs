#nullable enable

using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Nf3t.VintageStory.Sponge;

public class SpongeConfig
{
    // 1 => 3x3x3, 2 => 5x5x5, etc.
    public int AbsorbRadius = 1;
}

public class SpongeModSystem : ModSystem
{
    public static SpongeConfig? Config;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.RegisterItemClass("ItemSponge", typeof(ItemSponge));
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);

        Config = api.LoadModConfig<SpongeConfig>("Nf3tSponge.json");
        if (Config == null)
        {
            Config = new SpongeConfig();
            api.StoreModConfig(Config, "Nf3tSponge.json");
        }
    }
}