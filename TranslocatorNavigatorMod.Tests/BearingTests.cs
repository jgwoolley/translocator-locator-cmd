using Nf3t.VintageStory.Common;
using Xunit;

namespace Nf3t.VintageStory.TranslocatorNavigator.Tests;

/// <summary>The HUD arrow's angle: x is east, z is south; positive turns are to the right (clockwise from above).</summary>
public class BearingTests
{
    private const double Eps = 1e-9;

    [Theory]
    // Looking north (0, -1).
    [InlineData(0, -1, 0, -10, 0)] // target north: straight ahead
    [InlineData(0, -1, 10, 0, Math.PI / 2)] // east: to the right
    [InlineData(0, -1, -10, 0, -Math.PI / 2)] // west: to the left
    [InlineData(0, -1, 10, -10, Math.PI / 4)] // north-east: ahead, half right
    // Looking east (1, 0): south is to the right, north to the left.
    [InlineData(1, 0, 0, 10, Math.PI / 2)]
    [InlineData(1, 0, 0, -10, -Math.PI / 2)]
    public void TurnFromViewToTarget(double fx, double fz, double dx, double dz, double expected)
    {
        Assert.Equal(expected, Bearing.Relative(fx, fz, dx, dz)!.Value, Eps);
    }

    [Fact]
    public void BehindIsHalfATurn()
    {
        Assert.Equal(Math.PI, Math.Abs(Bearing.Relative(0, -1, 0, 10)!.Value), Eps);
    }

    [Fact]
    public void LengthDoesNotMatter()
    {
        Assert.Equal(Bearing.Relative(0, -1, 3, -4)!.Value, Bearing.Relative(0, -0.2, 300, -400)!.Value, Eps);
    }

    [Fact]
    public void NoDirectionWhenLookingStraightDownOrStandingOnTheTarget()
    {
        Assert.Null(Bearing.Relative(0, 0, 10, 0));
        Assert.Null(Bearing.Relative(0, -1, 0, 0));
    }
}
