#nullable enable

using System;
using System.Collections.Generic;

namespace Nf3t.VintageStory.EntropyRift;

/// <summary>
/// The rift's tuning, read from the server's <c>ModConfig/nf3tentropyrift.json</c> (written with these defaults the first time).
/// The defaults aim at about three temporal gears back per entropy gear (one temporal gear went into making it): bosses drop
/// one temporal gear each on average, corrupt and nightmare creatures about 0.05.
/// </summary>
public class RiftSettings
{
    /// <summary>False: a powered rift turns, but draws nothing out.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Pulses an entropy gear holds. Each pulse that brings something out uses one; when none are left (and its last
    /// boss is gone) the gear crumbles and the rift goes dormant.</summary>
    public int PulsesPerGear { get; set; } = 12;

    /// <summary>Real seconds between pulses, counted only while a player is within <see cref="PlayerRange"/>.</summary>
    public float PulseIntervalSeconds { get; set; } = 45;

    /// <summary>The rift only works while a player is this close (blocks).</summary>
    public float PlayerRange { get; set; } = 24;

    /// <summary>Ordinary creatures per pulse (inclusive range), besides any boss.</summary>
    public int CreaturesPerPulseMin { get; set; } = 1;
    public int CreaturesPerPulseMax { get; set; } = 2;

    /// <summary>Chance per pulse of a boss.</summary>
    public double BossChance { get; set; } = 0.2;

    /// <summary>Bosses every gear brings at least: once only that many pulses are left, each of them brings one.</summary>
    public int GuaranteedBosses { get; set; } = 2;

    /// <summary>Most of the rift's creatures alive at once (bosses included); a pulse waits while the rift is full.</summary>
    public int MaxAlive { get; set; } = 4;

    /// <summary>Most of the rift's bosses alive at once; a boss's pulse waits until the last one is dead.</summary>
    public int MaxBossesAlive { get; set; } = 1;

    /// <summary>Creatures climb out this far from the rift (blocks).</summary>
    public float SpawnDistanceMin { get; set; } = 3;
    public float SpawnDistanceMax { get; set; } = 10;

    /// <summary>Ordinary creatures and their weights.</summary>
    public List<WeightedCode> Creatures { get; set; } =
    [
        new("game:drifter-corrupt", 2), new("game:drifter-nightmare", 4),
        new("game:shiver-corrupt", 1), new("game:shiver-nightmare", 2),
        new("game:bowtorn-corrupt", 1), new("game:bowtorn-nightmare", 2),
    ];

    /// <summary>Bosses and their weights: the rust creatures above nightmare, which never spawn on their own outside storms.</summary>
    public List<WeightedCode> Bosses { get; set; } =
    [
        new("game:bowtorn-gearfoot", 1), new("game:drifter-double-headed", 1), new("game:shiver-deepsplit", 1),
        new("game:shiver-stilt", 1), new("game:shiver-bellhead", 1),
    ];
}

/// <summary>An entity code with a weight for <see cref="RiftRules.Pick"/>.</summary>
public class WeightedCode
{
    public WeightedCode() { }
    public WeightedCode(string code, double weight) { Code = code; Weight = weight; }
    public string Code { get; set; } = "";
    public double Weight { get; set; } = 1;
}

/// <summary>What one pulse brings out: maybe a boss, and some ordinary creatures.</summary>
public readonly record struct PulsePlan(bool Boss, int Creatures);

/// <summary>
/// What is left in the entropy gear in a powered rift: pulses, and the bosses it has brought out (for the guarantee).
/// Plain C#, no game types, so it can be unit tested.
/// </summary>
public class RiftCharge
{
    public int PulsesLeft { get; private set; }
    public int BossesDrawn { get; private set; }

    public RiftCharge(int pulsesLeft, int bossesDrawn = 0)
    {
        PulsesLeft = Math.Max(0, pulsesLeft);
        BossesDrawn = Math.Max(0, bossesDrawn);
    }

    public static RiftCharge NewGear(RiftSettings s) => new(s.PulsesPerGear);

    /// <summary>Bosses still owed by the guarantee.</summary>
    public int BossesOwed(RiftSettings s) => Math.Max(0, s.GuaranteedBosses - BossesDrawn);

    /// <summary>
    /// The next pulse, or null to wait (the gear is spent, the rift is full, or an owed boss must wait for the living one).
    /// <paramref name="alive"/> counts the rift's creatures alive now, <paramref name="bossesAlive"/> its bosses among them.
    /// </summary>
    public PulsePlan? Plan(RiftSettings s, Random rng, int alive, int bossesAlive)
    {
        if (PulsesLeft <= 0) return null;
        var room = s.MaxAlive - alive;
        if (room <= 0) return null;

        var owedNow = BossesOwed(s) >= PulsesLeft;   // every pulse left must bring one of the owed bosses
        var bossRoom = bossesAlive < s.MaxBossesAlive;
        if (owedNow && !bossRoom) return null;       // wait: spending this pulse without its boss would break the guarantee
        var boss = bossRoom && (owedNow || rng.NextDouble() < s.BossChance);

        var min = Math.Max(0, s.CreaturesPerPulseMin);
        var max = Math.Max(min, s.CreaturesPerPulseMax);
        var creatures = Math.Min(rng.Next(min, max + 1), room - (boss ? 1 : 0));
        if (creatures < 0) creatures = 0;
        if (!boss && creatures == 0) return null;
        return new PulsePlan(boss, creatures);
    }

    /// <summary>A pulse brought something out (<paramref name="bossSpawned"/>: including a boss): it is used up.</summary>
    public void Spend(bool bossSpawned)
    {
        if (PulsesLeft > 0) PulsesLeft--;
        if (bossSpawned) BossesDrawn++;
    }

    /// <summary>A boss left alive (despawned when players went away): the gear gets its pulse and its boss back.</summary>
    public void BossLeftAlive()
    {
        if (BossesDrawn > 0) BossesDrawn--;
        PulsesLeft++;
    }

    /// <summary>The gear is spent once no pulses are left and none of its bosses is still alive (one could still be given back).</summary>
    public bool Exhausted(int bossesAlive) => PulsesLeft <= 0 && bossesAlive <= 0;
}

public static class RiftRules
{
    /// <summary>A code picked by weight (null for an empty or weightless list).</summary>
    public static string? Pick(IReadOnlyList<WeightedCode> list, Random rng)
    {
        double total = 0;
        foreach (var w in list) total += Math.Max(0, w.Weight);
        if (total <= 0) return null;
        var r = rng.NextDouble() * total;
        foreach (var w in list)
        {
            r -= Math.Max(0, w.Weight);
            if (r < 0) return w.Code;
        }
        return list[^1].Code;
    }

    /// <summary>
    /// The location stability the rift leaves at <paramref name="distance"/> blocks: none at its core, rising to the world's
    /// own value at <paramref name="radius"/>. Never raises it.
    /// </summary>
    public static float DrainedStability(float stability, double distance, double radius)
    {
        if (radius <= 0 || distance >= radius) return stability;
        var t = (float)Math.Clamp(distance / radius, 0, 1);
        return Math.Min(stability, stability * t);
    }
}
