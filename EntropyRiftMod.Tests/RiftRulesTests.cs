using Xunit;

namespace Nf3t.VintageStory.EntropyRift.Tests;

public class RiftRulesTests
{
    private static readonly RiftSettings Defaults = new();

    /// <summary>A gear run to the end with every creature killed as it comes: (bosses, ordinary creatures, pulses used).</summary>
    private static (int bosses, int creatures, int pulses) RunGear(RiftSettings s, Random rng)
    {
        var charge = RiftCharge.NewGear(s);
        int bosses = 0, creatures = 0, pulses = 0;
        while (!charge.Exhausted(0))
        {
            var plan = charge.Plan(s, rng, alive: 0, bossesAlive: 0);
            Assert.NotNull(plan);   // with nothing alive a pulse never has to wait
            charge.Spend(plan.Value.Boss);
            bosses += plan.Value.Boss ? 1 : 0;
            creatures += plan.Value.Creatures;
            pulses++;
        }
        return (bosses, creatures, pulses);
    }

    [Fact]
    public void EveryGearBringsAtLeastTheGuaranteedBosses()
    {
        var rng = new Random(1);
        for (var i = 0; i < 20000; i++)
        {
            var (bosses, _, pulses) = RunGear(Defaults, rng);
            Assert.True(bosses >= Defaults.GuaranteedBosses, $"gear {i}: {bosses} bosses");
            Assert.Equal(Defaults.PulsesPerGear, pulses);
        }
    }

    [Fact]
    public void AGearReturnsAboutThreeTemporalGearsOnAverage()
    {
        // Drops from the game's entity files (1.22.7): bosses 1 temporal gear on average, corrupt 0.047, nightmare 0.053.
        // The default table is 1/3 corrupt and 2/3 nightmare by weight.
        const double perCreature = (0.046655 + 2 * 0.05332) / 3;
        var rng = new Random(2);
        double bosses = 0, creatures = 0;
        const int gears = 20000;
        for (var i = 0; i < gears; i++)
        {
            var r = RunGear(Defaults, rng);
            bosses += r.bosses;
            creatures += r.creatures;
        }
        var meanBosses = bosses / gears;
        var meanGears = (bosses + creatures * perCreature) / gears;
        Assert.InRange(meanBosses, 2.4, 2.9);
        Assert.InRange(meanGears, 3.0, 3.8);   // one temporal gear in, about three back
    }

    [Fact]
    public void APulseWaitsWhileTheRiftIsFull()
    {
        var charge = RiftCharge.NewGear(Defaults);
        Assert.Null(charge.Plan(Defaults, new Random(3), alive: Defaults.MaxAlive, bossesAlive: 0));
        Assert.Equal(Defaults.PulsesPerGear, charge.PulsesLeft);
    }

    [Fact]
    public void APulseNeverBringsMoreThanThereIsRoomFor()
    {
        var rng = new Random(4);
        for (var alive = 0; alive < Defaults.MaxAlive; alive++)
            for (var i = 0; i < 500; i++)
            {
                var plan = RiftCharge.NewGear(Defaults).Plan(Defaults, rng, alive, bossesAlive: 0);
                Assert.NotNull(plan);
                Assert.True(plan.Value.Creatures + (plan.Value.Boss ? 1 : 0) <= Defaults.MaxAlive - alive);
            }
    }

    [Fact]
    public void NoSecondBossWhileOneIsAlive()
    {
        var rng = new Random(5);
        for (var i = 0; i < 2000; i++)
        {
            var plan = RiftCharge.NewGear(Defaults).Plan(Defaults, rng, alive: 1, bossesAlive: 1);
            Assert.False(plan?.Boss ?? false);
        }
    }

    [Fact]
    public void AnOwedBossWaitsForTheLivingOneInsteadOfBeingSkipped()
    {
        // Two pulses left and both bosses still owed: each remaining pulse must bring one.
        var charge = new RiftCharge(pulsesLeft: 2, bossesDrawn: 0);
        Assert.Null(charge.Plan(Defaults, new Random(6), alive: 1, bossesAlive: 1));   // a boss is alive: wait
        var plan = charge.Plan(Defaults, new Random(6), alive: 0, bossesAlive: 0);
        Assert.True(plan?.Boss);
    }

    [Fact]
    public void ABossThatLeavesAliveGoesBackIntoTheGear()
    {
        var charge = new RiftCharge(pulsesLeft: 1, bossesDrawn: 1);
        charge.Spend(bossSpawned: true);
        Assert.Equal(0, charge.PulsesLeft);
        Assert.False(charge.Exhausted(bossesAlive: 1));   // spent, but its boss could still be given back
        charge.BossLeftAlive();
        Assert.Equal(1, charge.PulsesLeft);
        Assert.Equal(1, charge.BossesDrawn);
        Assert.False(charge.Exhausted(bossesAlive: 0));
    }

    [Fact]
    public void PickFollowsTheWeights()
    {
        var list = new List<WeightedCode> { new("a", 1), new("b", 3), new("c", 0) };
        var rng = new Random(7);
        var counts = new Dictionary<string, int> { ["a"] = 0, ["b"] = 0, ["c"] = 0 };
        for (var i = 0; i < 40000; i++) counts[RiftRules.Pick(list, rng)!]++;
        Assert.Equal(0, counts["c"]);
        Assert.InRange(counts["b"] / (double)counts["a"], 2.7, 3.3);
        Assert.Null(RiftRules.Pick(new List<WeightedCode>(), rng));
    }

    [Theory]
    [InlineData(0, 0f)]       // nothing left at the rift
    [InlineData(10, 0.6f)]    // half the radius: half the stability
    [InlineData(20, 1.2f)]    // the edge: untouched
    [InlineData(30, 1.2f)]
    public void TheDrainSinksStabilityTowardTheRift(double distance, float expected)
    {
        Assert.Equal(expected, RiftRules.DrainedStability(1.2f, distance, 20), 3);
    }

    [Fact]
    public void TheDrainNeverRaisesStability()
    {
        Assert.Equal(-0.5f, RiftRules.DrainedStability(-0.5f, 5, 20), 3);
    }
}
