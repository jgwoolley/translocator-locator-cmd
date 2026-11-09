using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Xunit;

namespace Nf3t.VintageStory.Statue.Tests;

/// <summary>
/// The seraph model faces -X (its eyes are on the low-x side of the head). These tests turn a point in front of a
/// statue with the game's own mesh rotation, to pin down where each compass angle makes the statue look.
/// </summary>
public class StatueFacingTests
{
    /// <summary>Where the point one step in front of a statue ends up (x east, z south), rounded.</summary>
    private static (int X, int Z) LooksAt(float angle)
    {
        var mesh = new MeshData(4, 6);
        mesh.AddVertex(-1, 0, 0, 0, 0);
        mesh.Rotate(new Vec3f(0, 0, 0), 0, angle, 0);
        return ((int)System.Math.Round(mesh.xyz[0]), (int)System.Math.Round(mesh.xyz[2]));
    }

    [Fact]
    public void TheCompassAnglesLookNorthEastSouthAndWest()
    {
        Assert.Equal((0, -1), LooksAt(StatueFacing.North));
        Assert.Equal((1, 0), LooksAt(StatueFacing.East));
        Assert.Equal((0, 1), LooksAt(StatueFacing.South));
        Assert.Equal((-1, 0), LooksAt(StatueFacing.West));
    }

    [Theory]
    [InlineData(0, 3, 0, 1)]    // the player is south of the statue: it looks south
    [InlineData(0, -3, 0, -1)]  // north
    [InlineData(3, 0, 1, 0)]    // east
    [InlineData(-3, 0, -1, 0)]  // west
    public void ARoundedPlacementAngleLooksTowardThePlayer(double dx, double dz, int expectedX, int expectedZ)
    {
        var angle = StatueFacing.Toward(dx, dz);
        Assert.Equal((expectedX, expectedZ), LooksAt(angle));
    }

    [Fact]
    public void TheCompassAnglesAreNamedAfterTheirDirection()
    {
        Assert.Equal("N", StatueFacing.PointOf(StatueFacing.North));
        Assert.Equal("E", StatueFacing.PointOf(StatueFacing.East));
        Assert.Equal("S", StatueFacing.PointOf(StatueFacing.South));
        Assert.Equal("W", StatueFacing.PointOf(StatueFacing.West));
    }

    [Theory]
    [InlineData(1, 1, "SW")]    // looking at a player to the south-west
    [InlineData(-1, 1, "SE")]
    [InlineData(1, -1, "NW")]
    [InlineData(-1, -1, "NE")]
    [InlineData(2, 1, "WSW")]
    public void APlacedStatuesFacingIsNamedAfterWhereItLooks(double dx, double dz, string expected)
    {
        // dx and dz point from the statue to the player, as when placing: +x is east, +z is south.
        Assert.Equal(expected, StatueFacing.PointOf(StatueFacing.Toward(-dx, dz)));
    }

    [Fact]
    public void AnglesAFullTurnApartAreTheSameDirection()
    {
        Assert.True(StatueFacing.Same(StatueFacing.North, StatueFacing.North + 2 * GameMath.PI));
        Assert.True(StatueFacing.Same(StatueFacing.South, StatueFacing.South - 2 * GameMath.PI));
        Assert.False(StatueFacing.Same(StatueFacing.North, StatueFacing.East));
    }

    [Theory]
    [InlineData(4, "E")]    // four right turns from north is east
    [InlineData(8, "S")]
    [InlineData(12, "W")]
    [InlineData(16, "N")]
    [InlineData(-4, "W")]   // left turns go the other way round
    [InlineData(-8, "S")]
    [InlineData(1, "NNE")]
    public void RightTurnsGoClockwiseRoundTheCompassAndLeftTurnsAnticlockwise(int rightSteps, string expected)
    {
        Assert.Equal(expected, StatueFacing.PointOf(StatueFacing.TurnedRight(StatueFacing.North, rightSteps)));
    }
}
