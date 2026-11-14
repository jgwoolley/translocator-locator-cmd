#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.EntropyRift;

/// <summary>
/// The Entropy Rift's state: powered (an entropy gear in the cradle, block variant <c>entropyrift-powered</c>) or dormant
/// (<c>entropyrift-empty</c>). Powered, its tower turns, temporal stability sinks round it, and while a player is near it
/// pulses, drawing rust creatures out of the rift: mostly corrupt and nightmare ones, now and then a boss (the tiers above
/// nightmare). The gear cannot be taken out; it holds a number of pulses (<see cref="RiftCharge"/>, tuned by
/// <see cref="RiftSettings"/>) and crumbles when they are spent, and the rift goes dormant.
/// </summary>
public class BlockEntityEntropyRift : BlockEntity
{
    private const string RiftAttr = "nf3tentropyrift";

    private DeviceAnimations? animations;
    private RiftCharge? charge;
    private float sinceLastPulse;
    private bool playerNear;

    /// <summary>The creatures this rift brought out and are not known to be gone (id, whether a boss).</summary>
    private readonly Dictionary<long, bool> creatures = new();

    /// <summary>The variant of the block actually placed here (on the client, the entity's own <c>Block</c> may lag a variant change).</summary>
    public string State => (Api?.World.BlockAccessor.GetBlock(Pos) ?? Block)?.Variant["state"] ?? "empty";
    public bool Powered => State == "powered";

    private EntropyRiftModSystem ModSystem => Api.ModLoader.GetModSystem<EntropyRiftModSystem>();
    private RiftSettings Settings => ModSystem.Settings;

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        if (api.Side == EnumAppSide.Client)
            animations = new DeviceAnimations(this, block => block.Variant["state"] == "powered" ? ["turn", "gearspin-reverse"] : []);
        else RegisterGameTickListener(OnServerTick, 1000);
        ModSystem.SetWorking(Pos, Powered);
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        animations?.OnTesselation();
        // The client learns of a variant change here (after every block change): keep its stability drain in step.
        Api?.ModLoader.GetModSystem<EntropyRiftModSystem>().SetWorking(Pos, Powered);
        return base.OnTesselation(mesher, tessThreadTesselator);
    }

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();
        Api?.ModLoader.GetModSystem<EntropyRiftModSystem>().SetWorking(Pos, false);
    }

    public override void OnBlockUnloaded()
    {
        base.OnBlockUnloaded();
        Api?.ModLoader.GetModSystem<EntropyRiftModSystem>().SetWorking(Pos, false);
    }

    private void SetState(string state)
    {
        var block = Api.World.GetBlock(Block.CodeWithVariant("state", state));
        if (block == null) return;
        Api.World.BlockAccessor.ExchangeBlock(block.Id, Pos);
        Block = block;
        Api.World.BlockAccessor.MarkBlockDirty(Pos);
        ModSystem.SetWorking(Pos, state == "powered");
        MarkDirty(true);
    }

    /// <summary>Sets an entropy gear in the cradle (server). It stays there until its pulses are spent.</summary>
    public void InsertEntropyGear()
    {
        charge = RiftCharge.NewGear(Settings);
        sinceLastPulse = 0;
        creatures.Clear();
        SetState("powered");
        Api.World.PlaySoundAt(new AssetLocation("sounds/effect/rift"), Pos, 0.5, null, true, 16, 0.6f);
    }

    public override void OnBlockBroken(IPlayer? byPlayer = null)
    {
        base.OnBlockBroken(byPlayer);
        // The gear is spent into the rift, not given back: breaking the rift does not return it.
        if (Api.Side == EnumAppSide.Server && Powered) Burst(Pos.ToVec3d().Add(0.5, 0.8, 0.5), 40);
    }

    // ------------------------------------------------------------------ server: pulses

    private void OnServerTick(float dt)
    {
        if (!Powered) return;
        charge ??= RiftCharge.NewGear(Settings);   // a rift powered before this version, or by a world edit

        var (alive, bossesAlive) = CountAlive();
        if (charge.Exhausted(bossesAlive))
        {
            Crumble();
            return;
        }
        if (!Settings.Enabled) return;

        var near = Api.World.NearestPlayer(Pos.X + 0.5, Pos.Y + 1, Pos.Z + 0.5) is { Entity: { } e }
                   && e.Pos.DistanceTo(Pos.ToVec3d().Add(0.5, 1, 0.5)) <= Settings.PlayerRange;
        if (near != playerNear)
        {
            playerNear = near;
            MarkDirty();
        }
        if (!near) return;

        sinceLastPulse += dt;
        if (sinceLastPulse < Settings.PulseIntervalSeconds) return;

        var plan = charge.Plan(Settings, Api.World.Rand, alive, bossesAlive);
        if (plan == null) return;   // waiting (full, or an owed boss waits for the living one); try again next tick
        sinceLastPulse = 0;
        Pulse(plan.Value);
    }

    private (int alive, int bossesAlive) CountAlive()
    {
        int alive = 0, bosses = 0;
        foreach (var (id, boss) in creatures)
        {
            // Only loaded, living creatures count; an unloaded one is still tracked and counts again when it comes back.
            if (Api.World.GetEntityById(id) is not { Alive: true }) continue;
            alive++;
            if (boss) bosses++;
        }
        return (alive, bosses);
    }

    private void Pulse(PulsePlan plan)
    {
        var bossSpawned = plan.Boss && SpawnOne(RiftRules.Pick(Settings.Bosses, Api.World.Rand), true);
        var spawned = bossSpawned ? 1 : 0;
        for (var i = 0; i < plan.Creatures; i++)
            if (SpawnOne(RiftRules.Pick(Settings.Creatures, Api.World.Rand), false)) spawned++;
        if (spawned == 0) return;   // nowhere to stand: the pulse is not used up

        charge!.Spend(bossSpawned);
        Burst(Pos.ToVec3d().Add(0.5, 1.9, 0.5), bossSpawned ? 50 : 25);
        Api.World.PlaySoundAt(new AssetLocation("sounds/effect/rift"), Pos, 0.5, null, true, 32, bossSpawned ? 1 : 0.7f);
        MarkDirty();
    }

    /// <summary>One creature out of the rift on solid ground near it, placed the way the game's storm spawner places its
    /// creatures (walk down to the ground, check the creature's spawn box is clear). False if it found no room.</summary>
    private bool SpawnOne(string? code, bool boss)
    {
        if (code == null) return false;
        var type = Api.World.GetEntityType(new AssetLocation(code));
        if (type == null)
        {
            Api.Logger.Warning("[nf3tentropyrift] unknown creature {0} in ModConfig/{1}", code, EntropyRiftModSystem.ConfigFile);
            return false;
        }

        var rand = Api.World.Rand;
        var tester = new CollisionTester();
        var at = new Vec3d();
        var ground = new BlockPos(Pos.dimension);
        for (var attempt = 0; attempt < 12; attempt++)
        {
            var angle = rand.NextDouble() * GameMath.TWOPI;
            var dist = Settings.SpawnDistanceMin + rand.NextDouble() * Math.Max(0, Settings.SpawnDistanceMax - Settings.SpawnDistanceMin);
            at.Set((int)(Pos.X + 0.5 + Math.Cos(angle) * dist) + 0.5, Pos.Y + 3 + 0.001, (int)(Pos.Z + 0.5 + Math.Sin(angle) * dist) + 0.5);
            ground.Set((int)at.X, (int)at.Y, (int)at.Z);
            var steps = 0;
            while (Api.World.BlockAccessor.GetBlock(ground.DownCopy()).Id == 0 && steps++ < 10)
            {
                ground.Y--;
                at.Y--;
            }
            if (steps > 10 || !Api.World.BlockAccessor.IsValidPos(ground)) continue;
            if (Api.World.BlockAccessor.GetBlock(ground, BlockLayersAccess.Fluid).IsLiquid()) continue;
            if (tester.IsColliding(Api.World.BlockAccessor, type.SpawnCollisionBox.OmniNotDownGrowBy(0.1f), at, false)) continue;

            var entity = Api.World.ClassRegistry.CreateEntity(type);
            entity.Pos.SetPosWithDimension(at);
            entity.Pos.SetYaw((float)(rand.NextDouble() * GameMath.TWOPI));
            entity.PositionBeforeFalling.Set(entity.Pos.X, entity.Pos.Y, entity.Pos.Z);
            entity.Attributes.SetString("origin", RiftAttr);
            entity.Attributes.SetBlockPos(RiftAttr + "-pos", Pos);
            Api.World.SpawnEntity(entity);
            entity.Attributes.SetBool("ignoreDaylightFlee", true);   // works on the surface by day, like storm creatures
            creatures[entity.EntityId] = boss;
            Burst(at.AddCopy(0, 0.6, 0), boss ? 30 : 12);
            return true;
        }
        return false;
    }

    /// <summary>The rift this entity came out of, if it did.</summary>
    public static bool TryGetRiftPos(Entity entity, out BlockPos pos)
    {
        pos = entity.Attributes.GetBlockPos(RiftAttr + "-pos");
        return pos != null && entity.Attributes.GetString("origin") == RiftAttr;
    }

    /// <summary>One of this rift's creatures despawned (server). A boss that left alive goes back into the gear.</summary>
    public void OnCreatureGone(long entityId, EnumDespawnReason reason)
    {
        if (!creatures.TryGetValue(entityId, out var boss)) return;
        // Unloaded with its chunk (or the last player left): it still exists and will be back.
        if (reason is EnumDespawnReason.Unload or EnumDespawnReason.Disconnect) return;
        creatures.Remove(entityId);
        var died = reason is EnumDespawnReason.Death or EnumDespawnReason.Combusted;
        if (boss && !died && charge != null && Powered) charge.BossLeftAlive();
        MarkDirty();
    }

    /// <summary>The gear's pulses are spent: it crumbles and the rift goes dormant.</summary>
    private void Crumble()
    {
        charge = null;
        creatures.Clear();
        Burst(Pos.ToVec3d().Add(0.5, 0.8, 0.5), 60);
        Api.World.PlaySoundAt(new AssetLocation("sounds/effect/translocate-breakdown"), Pos, 0.5, null, true, 24);
        SetState("empty");
    }

    // ------------------------------------------------------------------ effects (spawned on the server, sent to clients)

    private static readonly SimpleParticleProperties Motes = new(
        1, 1, ColorUtil.ToRgba(200, 255, 120, 30), new Vec3d(), new Vec3d(),
        new Vec3f(-1.2f, -0.4f, -1.2f), new Vec3f(1.2f, 1.6f, 1.2f), 1.4f, -0.05f, 0.2f, 0.6f, EnumParticleModel.Cube)
    {
        WithTerrainCollision = false,
        SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -0.3f)
    };

    /// <summary>A burst of the storm's orange and black at <paramref name="at"/>.</summary>
    private void Burst(Vec3d at, int count)
    {
        Motes.MinPos.Set(at.X - 0.3, at.Y - 0.3, at.Z - 0.3);
        Motes.AddPos.Set(0.6, 0.6, 0.6);
        Motes.MinQuantity = count * 0.6f;
        Motes.AddQuantity = count * 0.4f;
        Motes.Color = ColorUtil.ToRgba(220, 255, 120, 20);    // ARGB: orange
        Api.World.SpawnParticles(Motes);
        Motes.MinQuantity = count * 0.3f;
        Motes.AddQuantity = count * 0.2f;
        Motes.Color = ColorUtil.ToRgba(220, 20, 14, 12);      // near black
        Api.World.SpawnParticles(Motes);
    }

    // ------------------------------------------------------------------ info and saving

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        if (!Powered)
        {
            dsc.AppendLine(Lang.Get("nf3tentropyrift:rift-info-dormant"));
            return;
        }
        dsc.AppendLine(Lang.Get("nf3tentropyrift:rift-info-powered"));
        if (charge != null) dsc.AppendLine(Lang.Get("nf3tentropyrift:rift-info-pulses", charge.PulsesLeft));
        if (!playerNear) dsc.AppendLine(Lang.Get("nf3tentropyrift:rift-info-idle"));
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        if (charge != null)
        {
            tree.SetInt("pulsesLeft", charge.PulsesLeft);
            tree.SetInt("bossesDrawn", charge.BossesDrawn);
        }
        else
        {
            tree.RemoveAttribute("pulsesLeft");
            tree.RemoveAttribute("bossesDrawn");
        }
        tree.SetFloat("sinceLastPulse", sinceLastPulse);
        tree.SetBool("playerNear", playerNear);
        tree.SetString("creatures", string.Join(",", creatures.Select(c => c.Key + (c.Value ? "b" : ""))));
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);
        charge = tree.HasAttribute("pulsesLeft") ? new RiftCharge(tree.GetInt("pulsesLeft"), tree.GetInt("bossesDrawn")) : null;
        sinceLastPulse = tree.GetFloat("sinceLastPulse");
        playerNear = tree.GetBool("playerNear");
        creatures.Clear();
        foreach (var part in (tree.GetString("creatures") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var boss = part.EndsWith('b');
            if (long.TryParse(boss ? part[..^1] : part, out var id)) creatures[id] = boss;
        }
    }
}
