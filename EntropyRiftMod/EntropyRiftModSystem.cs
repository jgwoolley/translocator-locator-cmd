#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Nf3t.VintageStory.EntropyRift;

public class EntropyRiftModSystem : ModSystem
{
    public const string ConfigFile = "nf3tentropyrift.json";

    /// <summary>Blocks round a working rift where temporal stability sinks (none at the rift, normal at this distance).</summary>
    public const double DrainRadius = 20;

    /// <summary>Players near a working rift never drop below this stability (outside storms): fog, rust rain and a little glitch,
    /// but not the poison that starts below 0.13.</summary>
    public const double StabilityFloor = 0.15;

    /// <summary>The server's tuning (defaults on the client, which does not spawn anything).</summary>
    public RiftSettings Settings { get; private set; } = new();

    private ICoreAPI? api;

    // Working rifts on this side, as an array swapped on change: the stability hook reads it every tick for every player, on
    // both sides and off the main thread, so it takes no locks.
    private readonly object riftsLock = new();
    private readonly HashSet<BlockPos> working = [];
    private Vec3d[] workingCentres = [];

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        this.api = api;
        api.RegisterBlockClass("BlockStormSiphon", typeof(BlockStormSiphon));
        api.RegisterBlockEntityClass("StormSiphon", typeof(BlockEntityStormSiphon));
        api.RegisterBlockClass("BlockEntropyRift", typeof(BlockEntropyRift));
        api.RegisterBlockEntityClass("EntropyRift", typeof(BlockEntityEntropyRift));
        api.RegisterItemClass("ItemEntropyGear", typeof(ItemEntropyGear));

        // Both sides: the client predicts stability too, so it must sink there as well or the effects and the gauge disagree.
        var stability = api.ModLoader.GetModSystem<SystemTemporalStability>();
        if (stability != null) stability.OnGetTemporalStability += Drain;
    }

    public override void StartServerSide(ICoreServerAPI sapi)
    {
        base.StartServerSide(sapi);
        try
        {
            Settings = sapi.LoadModConfig<RiftSettings>(ConfigFile) ?? new RiftSettings();
        }
        catch (Exception e)
        {
            sapi.Logger.Error("[nf3tentropyrift] could not read ModConfig/{0}, using defaults: {1}", ConfigFile, e.Message);
            Settings = new RiftSettings();
        }
        sapi.StoreModConfig(Settings, ConfigFile);

        sapi.Event.OnEntityDespawn += OnEntityDespawn;
        sapi.Event.RegisterGameTickListener(_ => HoldStabilityFloor(sapi), 1000);
    }

    // ------------------------------------------------------------------ working rifts

    /// <summary>Called by a rift on either side when it starts or stops working (or is unloaded).</summary>
    public void SetWorking(BlockPos pos, bool isWorking)
    {
        lock (riftsLock)
        {
            var changed = isWorking ? working.Add(pos.Copy()) : working.Remove(pos);
            if (changed) workingCentres = working.Select(p => new Vec3d(p.X + 0.5, p.InternalY + 1, p.Z + 0.5)).ToArray();
        }
    }

    private float Drain(float stability, double x, double y, double z)
    {
        var centres = workingCentres;
        foreach (var c in centres)
        {
            double dx = x - c.X, dy = y - c.Y, dz = z - c.Z;
            var d2 = dx * dx + dy * dy + dz * dz;
            if (d2 < DrainRadius * DrainRadius) stability = RiftRules.DrainedStability(stability, Math.Sqrt(d2), DrainRadius);
        }
        return stability;
    }

    private bool NearWorkingRift(double x, double y, double z)
    {
        foreach (var c in workingCentres)
        {
            double dx = x - c.X, dy = y - c.Y, dz = z - c.Z;
            if (dx * dx + dy * dy + dz * dz < DrainRadius * DrainRadius) return true;
        }
        return false;
    }

    /// <summary>
    /// The game lowers a player's own stability for as long as the place they stand is below 1, all the way to 0 and its
    /// poison. Near a working rift it stops at <see cref="StabilityFloor"/>. Not during storms: a rift is no shelter from one.
    /// </summary>
    private void HoldStabilityFloor(ICoreServerAPI sapi)
    {
        if (workingCentres.Length == 0 || StormForecast.StormActive(sapi)) return;
        foreach (var player in sapi.World.AllOnlinePlayers)
        {
            var entity = player.Entity;
            if (entity == null || !NearWorkingRift(entity.Pos.X, entity.Pos.Y, entity.Pos.Z)) continue;
            var own = entity.WatchedAttributes.GetDouble("temporalStability", 1);
            if (own < StabilityFloor) entity.WatchedAttributes.SetDouble("temporalStability", StabilityFloor);
        }
    }

    // ------------------------------------------------------------------ the rift's creatures leaving

    private void OnEntityDespawn(Entity entity, EntityDespawnData? data)
    {
        if (api == null || !BlockEntityEntropyRift.TryGetRiftPos(entity, out var pos)) return;
        if (api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityEntropyRift rift)
            rift.OnCreatureGone(entity.EntityId, data?.Reason ?? EnumDespawnReason.Removed);
    }
}
