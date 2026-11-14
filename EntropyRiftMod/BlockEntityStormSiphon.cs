#nullable enable

using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.EntropyRift;

/// <summary>
/// The Storm Siphon's state. Which gear sits in the cradle is the block variant (<c>stormsiphon-empty|temporal|entropy</c>);
/// this entity counts how long the temporal gear has been exposed to a temporal storm. After <see cref="RequiredStormHours"/>
/// in-game hours of storm (across any number of storms), with open sky above, the gear becomes an entropy gear.
/// </summary>
public class BlockEntityStormSiphon : BlockEntity
{
    /// <summary>In-game hours of temporal storm the gear must drink. The shortest storms last 2.4 hours (0.1 days).</summary>
    public const double RequiredStormHours = 2.0;

    /// <summary>Storm hours gathered so far. Lost when the temporal gear is taken out.</summary>
    public double StormHours { get; private set; }

    /// <summary>True while a storm is running, a temporal gear is in the cradle and the sky is open (synced for particles).</summary>
    public bool Charging { get; private set; }

    /// <summary>Spins the gear while one is in the cradle (faster while charging). Client only.</summary>
    private DeviceAnimations? animations;

    /// <summary>Gear spin speed while drawing in a storm, relative to idle.</summary>
    private const float ChargingSpinSpeed = 3f;

    private double lastTotalHours;
    private int ticksSinceSync;

    /// <summary>The variant of the block actually placed here (on the client, the entity's own <c>Block</c> may lag a variant change).</summary>
    public string State => (Api?.World.BlockAccessor.GetBlock(Pos) ?? Block)?.Variant["state"] ?? "empty";
    public bool HasTemporalGear => State == "temporal";
    public bool HasEntropyGear => State == "entropy";
    public double Progress => Math.Clamp(StormHours / RequiredStormHours, 0, 1);

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        lastTotalHours = api.World.Calendar.TotalHours;
        if (api.Side == EnumAppSide.Server) RegisterGameTickListener(OnServerTick, 1000);
        else
        {
            RegisterGameTickListener(OnClientTick, 100);
            // The temporal gear spins one way, the entropy gear the other (each variant's shape has its own animation).
            // The temporal gear turns only while the storm is filling it; the entropy gear turns all the time.
            animations = new DeviceAnimations(this, block => block.Variant["state"] switch
            {
                "temporal" => Charging ? ["gearspin"] : [],
                "entropy" => ["gearspin-reverse"],
                _ => []
            });
            animations.SetSpeed(Charging ? ChargingSpinSpeed : 1);
        }
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        animations?.OnTesselation();
        return base.OnTesselation(mesher, tessThreadTesselator);
    }

    /// <summary>The storm can only reach the rod if nothing above the device blocks the rain.</summary>
    public bool SkyOpen()
    {
        // The device itself is two blocks tall (the multiblock's upper half sits at Pos.Y + 1).
        return Api.World.BlockAccessor.GetRainMapHeightAt(Pos) <= Pos.Y + 1;
    }

    private void OnServerTick(float dt)
    {
        var now = Api.World.Calendar.TotalHours;
        // A chunk that was unloaded (or a long server lag) must not count the whole gap as storm time.
        var hours = Math.Clamp(now - lastTotalHours, 0, 0.5);
        lastTotalHours = now;

        var charging = HasTemporalGear && StormForecast.StormActive(Api) && SkyOpen();
        if (charging) StormHours += hours;

        if (charging && StormHours >= RequiredStormHours)
        {
            Convert();
            return;
        }

        if (charging != Charging || (charging && ++ticksSinceSync >= 5))
        {
            Charging = charging;
            ticksSinceSync = 0;
            MarkDirty();
        }
    }

    private void Convert()
    {
        StormHours = 0;
        Charging = false;
        SetState("entropy");
        Api.World.PlaySoundAt(new AssetLocation("sounds/effect/portal"), Pos, 0.5, null, true, 24);
    }

    /// <summary>Swaps the block to another variant without losing this entity.</summary>
    public void SetState(string state)
    {
        var block = Api.World.GetBlock(Block.CodeWithVariant("state", state));
        if (block == null) return;
        Api.World.BlockAccessor.ExchangeBlock(block.Id, Pos);
        Block = block;
        Api.World.BlockAccessor.MarkBlockDirty(Pos);
        MarkDirty(true);
    }

    public void InsertTemporalGear()
    {
        StormHours = 0;
        lastTotalHours = Api.World.Calendar.TotalHours;
        SetState("temporal");
    }

    /// <summary>Takes the gear out of the cradle (null if there is none). A temporal gear loses its gathered storm.</summary>
    public ItemStack? TakeGear()
    {
        var code = HasTemporalGear ? BlockStormSiphon.TemporalGearCode : HasEntropyGear ? BlockStormSiphon.EntropyGearCode : null;
        var item = code == null ? null : Api.World.GetItem(new AssetLocation(code));
        if (item == null) return null;
        StormHours = 0;
        Charging = false;
        SetState("empty");
        return new ItemStack(item);
    }

    public override void OnBlockBroken(IPlayer? byPlayer = null)
    {
        base.OnBlockBroken(byPlayer);
        if (Api.Side != EnumAppSide.Server) return;
        var code = HasTemporalGear ? BlockStormSiphon.TemporalGearCode : HasEntropyGear ? BlockStormSiphon.EntropyGearCode : null;
        var item = code == null ? null : Api.World.GetItem(new AssetLocation(code));
        if (item != null) Api.World.SpawnItemEntity(new ItemStack(item), Pos.ToVec3d().Add(0.5, 0.6, 0.5));
    }

    // ------------------------------------------------------------------ client: energy running down the spike from the dish

    private static readonly SimpleParticleProperties FeedParticles = new(
        1, 1, ColorUtil.ToRgba(220, 80, 220, 200), new Vec3d(), new Vec3d(),
        new Vec3f(-0.02f, -0.6f, -0.02f), new Vec3f(0.02f, -0.45f, 0.02f), 1.1f, 0, 0.12f, 0.22f, EnumParticleModel.Cube)
    {
        WithTerrainCollision = false,
        LifeLength = 1.3f,
        SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -0.1f)
    };

    private void OnClientTick(float dt)
    {
        if (!Charging || Api.World.Rand.NextDouble() > 0.6) return;
        // Teal at first, shifting to the storm's red-orange as the gear fills.
        var t = (float)Progress;
        FeedParticles.Color = ColorUtil.ToRgba(220, (int)GameMath.Lerp(80, 240, t), (int)GameMath.Lerp(220, 90, t), (int)GameMath.Lerp(200, 40, t));
        FeedParticles.MinPos.Set(Pos.X + 0.47, Pos.Y + 1.28, Pos.Z + 0.47);   // just under the dish hub
        FeedParticles.AddPos.Set(0.06, 0.06, 0.06);
        Api.World.SpawnParticles(FeedParticles);
    }

    // ------------------------------------------------------------------ info and saving

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        if (HasEntropyGear)
        {
            dsc.AppendLine(Lang.Get("nf3tentropyrift:siphon-info-done"));
            return;
        }
        if (!HasTemporalGear)
        {
            dsc.AppendLine(Lang.Get("nf3tentropyrift:siphon-info-empty"));
            return;
        }

        dsc.AppendLine(Lang.Get("nf3tentropyrift:siphon-info-progress", Progress * 100));
        dsc.AppendLine(StormForecast.Describe(Api));
        if (Charging) dsc.AppendLine(Lang.Get("nf3tentropyrift:siphon-info-charging"));
        else if (!SkyOpen()) dsc.AppendLine(Lang.Get("nf3tentropyrift:siphon-info-nosky"));
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetDouble("stormHours", StormHours);
        tree.SetDouble("lastTotalHours", lastTotalHours);
        tree.SetBool("charging", Charging);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);
        StormHours = tree.GetDouble("stormHours");
        lastTotalHours = tree.GetDouble("lastTotalHours", worldForResolving.Calendar?.TotalHours ?? 0);
        var wasCharging = Charging;
        Charging = tree.GetBool("charging");
        animations?.SetSpeed(Charging ? ChargingSpinSpeed : 1);
        if (Charging != wasCharging) animations?.Refresh();
    }
}
