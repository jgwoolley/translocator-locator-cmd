using Nf3t.VintageStory.Common;
using Xunit;

namespace Nf3t.VintageStory.TranslocatorNavigator.Tests;

public class RouteTests
{
    private static readonly SimplePos Start = new(0, 100, 0);
    private static readonly SimplePos Entrance = new(30, 100, 40); // 50 blocks from Start
    private static readonly SimplePos Exit = new(5000, 100, 5000);
    private static readonly SimplePos Goal = new(5000, 100, 5100);

    private static readonly Dictionary<SimplePos, SimplePos?> Translocators = new()
    {
        [Entrance] = Exit,
        [Exit] = Entrance,
        [new SimplePos(9, 9, 9)] = null
    };

    private static IReadOnlyList<RouteStep> Steps() => RouteSteps.Build([Start, Entrance, Exit, Goal], Translocators);

    [Fact]
    public void BuildLabelsWalksAndTeleports()
    {
        var steps = Steps();

        Assert.Equal(
        [
            new RouteStep(StepKind.Walk, Start, Entrance, 50),
            new RouteStep(StepKind.Teleport, Entrance, Exit, 0),
            new RouteStep(StepKind.Walk, Exit, Goal, 100)
        ], steps);
    }

    [Fact]
    public void BuildTreatsReverseLinkAsTeleport()
    {
        var oneWay = new Dictionary<SimplePos, SimplePos?> { [Exit] = Entrance };

        var steps = RouteSteps.Build([Entrance, Exit], oneWay);

        Assert.Equal(StepKind.Teleport, Assert.Single(steps).Kind);
    }

    [Fact]
    public void BuildOfDirectPathIsOneWalk()
    {
        var step = Assert.Single(RouteSteps.Build([Start, Goal], Translocators));

        Assert.Equal(StepKind.Walk, step.Kind);
        Assert.Equal((int)Start.DistanceTo(Goal), step.Distance);
    }

    [Fact]
    public void ProgressAdvancesOnArrival()
    {
        var progress = new RouteProgress(Steps());

        Assert.False(progress.Update(new SimplePos(10, 100, 10)));
        Assert.Equal(0, progress.Index);

        Assert.True(progress.Update(new SimplePos(32, 101, 41)));
        Assert.Equal(StepKind.Teleport, progress.Current!.Value.Kind);
    }

    [Fact]
    public void ProgressSkipsAheadWhenComingOutOfTheExit()
    {
        var progress = new RouteProgress(Steps());

        progress.Update(new SimplePos(5001, 100, 5001));

        Assert.Equal(2, progress.Index);
        Assert.Equal(Goal, progress.Current!.Value.To);
    }

    [Fact]
    public void ProgressNeverGoesBack()
    {
        var progress = new RouteProgress(Steps());
        progress.Update(Exit);

        Assert.False(progress.Update(Entrance));
        Assert.Equal(2, progress.Index);
    }

    [Fact]
    public void ProgressArrivesAtGoal()
    {
        var progress = new RouteProgress(Steps());

        progress.Update(Goal);

        Assert.True(progress.IsArrived);
        Assert.Null(progress.Current);
    }

    [Fact]
    public void FromRelativeRoundTripsWithRelativeString()
    {
        var spawn = new SimplePos(512000, 110, 511000);

        var pos = RouteSteps.FromRelative(-120, 72, 45, spawn);

        Assert.Equal(new SimplePos(511880, 72, 511045), pos);
        Assert.Equal("-120, 72, 45", pos.ToRelativeString(spawn));
    }
}
