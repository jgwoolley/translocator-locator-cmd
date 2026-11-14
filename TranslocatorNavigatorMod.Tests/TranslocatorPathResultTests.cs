using Nf3t.VintageStory.Common;
using Xunit;

namespace Nf3t.VintageStory.TranslocatorNavigator.Tests;

/// <summary>
/// The translocator route search (<see cref="TranslocatorPathResult"/>). These pin down its current behavior, which uses the
/// Dijkstra.NET package, so that a replacement implementation can be checked against them.
/// <para>
/// Distances are chosen as 3-4-5 triangles (30, 40 → 50) so every walk is a whole number of blocks: the search casts each
/// walk to <c>int</c>, so the route length is the sum of the truncated walks. Teleports cost 0.
/// </para>
/// </summary>
public class TranslocatorPathResultTests
{
    private static readonly SimplePos Start = new(0, 100, 0);

    private static TranslocatorPathResult Find(Dictionary<SimplePos, SimplePos?> translocators, SimplePos start, SimplePos goal) =>
        new(translocators, start, goal);

    [Fact]
    public void WithoutTranslocatorsTheRouteIsOneWalk()
    {
        var goal = new SimplePos(300, 100, 400);

        var result = Find([], Start, goal);

        Assert.True(result.IsFounded());
        Assert.Equal([Start, goal], result.Path);
        Assert.Equal(500, result.GetTotalDistance());
        Assert.Equal(500, result.GetBirdsEyeDistance());
        Assert.Equal(goal, result.GetNextStep());
    }

    [Fact]
    public void UsesATranslocatorThatShortensTheTrip()
    {
        var entrance = new SimplePos(30, 100, 40); // 50 from Start
        var exit = new SimplePos(5000, 100, 5000);
        var goal = new SimplePos(5000, 100, 5100); // 100 from the exit
        var translocators = new Dictionary<SimplePos, SimplePos?> { [entrance] = exit, [exit] = entrance };

        var result = Find(translocators, Start, goal);

        Assert.Equal([Start, entrance, exit, goal], result.Path);
        Assert.Equal(150, result.GetTotalDistance());
        Assert.Equal((long)Start.DistanceTo(goal), result.GetBirdsEyeDistance());
        Assert.Equal(entrance, result.GetNextStep());
    }

    [Fact]
    public void WalksWhenTheTranslocatorIsADetour()
    {
        // Entrance 500 away, exit beyond the goal: walking straight (500) beats 500 + 0 + 500.
        var entrance = new SimplePos(-300, 100, -400);
        var exit = new SimplePos(600, 100, 800);
        var goal = new SimplePos(300, 100, 400);
        var translocators = new Dictionary<SimplePos, SimplePos?> { [entrance] = exit, [exit] = entrance };

        var result = Find(translocators, Start, goal);

        Assert.Equal([Start, goal], result.Path);
        Assert.Equal(500, result.GetTotalDistance());
    }

    [Fact]
    public void TranslocatorsWorkBothWaysEvenIfOnlyOneEndIsRecorded()
    {
        // Only entrance → exit is known, but the route goes exit → entrance.
        var entrance = new SimplePos(5000, 100, 5000);
        var exit = new SimplePos(30, 100, 40); // 50 from Start
        var goal = new SimplePos(5030, 100, 5040); // 50 from the entrance
        var translocators = new Dictionary<SimplePos, SimplePos?> { [entrance] = exit };

        var result = Find(translocators, Start, goal);

        Assert.Equal([Start, exit, entrance, goal], result.Path);
        Assert.Equal(100, result.GetTotalDistance());
    }

    [Fact]
    public void ChainsTranslocatorsWhoseEndsAreNearEachOther()
    {
        var a = new SimplePos(30, 100, 40); // 50 from Start
        var aExit = new SimplePos(10000, 100, 0);
        var b = new SimplePos(10030, 100, 40); // 50 from a's exit
        var bExit = new SimplePos(20000, 100, 0);
        var goal = new SimplePos(20000, 100, 100); // 100 from b's exit
        var translocators = new Dictionary<SimplePos, SimplePos?>
        {
            [a] = aExit, [aExit] = a,
            [b] = bExit, [bExit] = b
        };

        var result = Find(translocators, Start, goal);

        Assert.Equal([Start, a, aExit, b, bExit, goal], result.Path);
        Assert.Equal(200, result.GetTotalDistance());
    }

    [Fact]
    public void IgnoresTranslocatorsWithoutAKnownTarget()
    {
        // An unlinked translocator right on the way is no shortcut.
        var unlinked = new SimplePos(30, 100, 40);
        var goal = new SimplePos(300, 100, 400);
        var translocators = new Dictionary<SimplePos, SimplePos?> { [unlinked] = null };

        var result = Find(translocators, Start, goal);

        Assert.Equal([Start, goal], result.Path);
        Assert.Equal(500, result.GetTotalDistance());
    }

    [Fact]
    public void PicksTheShorterOfTwoTranslocatorRoutes()
    {
        var goal = new SimplePos(9000, 100, 9000);
        var nearEntrance = new SimplePos(30, 100, 40); // 50 from Start
        var nearExit = new SimplePos(8970, 100, 8960); // 50 from the goal
        var farEntrance = new SimplePos(300, 100, 400); // 500 from Start
        var farExit = new SimplePos(8700, 100, 8600); // 500 from the goal
        var translocators = new Dictionary<SimplePos, SimplePos?>
        {
            [farEntrance] = farExit, [farExit] = farEntrance,
            [nearEntrance] = nearExit, [nearExit] = nearEntrance
        };

        var result = Find(translocators, Start, goal);

        Assert.Equal([Start, nearEntrance, nearExit, goal], result.Path);
        Assert.Equal(100, result.GetTotalDistance());
    }

    [Fact]
    public void WalkDistancesAreTruncatedToWholeBlocks()
    {
        // Each walk is cast to int: both walks are √3 ≈ 1.73 blocks → 1, so the route counts 1 + 0 + 1 = 2, not 3.46.
        var entrance = new SimplePos(1, 101, 1);
        var exit = new SimplePos(9000, 100, 9000);
        var goal = new SimplePos(9001, 101, 9001);
        var translocators = new Dictionary<SimplePos, SimplePos?> { [entrance] = exit, [exit] = entrance };

        var result = Find(translocators, Start, goal);

        Assert.Equal([Start, entrance, exit, goal], result.Path);
        Assert.Equal(2, result.GetTotalDistance());
    }

    [Fact]
    public void StandingOnTheGoalIsARouteWithNoNextStep()
    {
        var result = Find([], Start, Start);

        Assert.True(result.IsFounded());
        Assert.Equal([Start], result.Path);
        Assert.Equal(0, result.GetTotalDistance());
        Assert.Null(result.GetNextStep());
    }
}
