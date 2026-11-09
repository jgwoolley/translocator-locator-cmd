using System;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.Statue;

/// <summary>
/// Which way a statue looks, as the angle the mesh is turned about the vertical axis. The seraph model faces west
/// (-X) when the angle is 0, and a positive angle turns it from west towards south (+Z).
/// </summary>
public static class StatueFacing
{
    public const float West = 0;
    public const float South = GameMath.PIHALF;
    public const float East = GameMath.PI;
    public const float North = -GameMath.PIHALF;

    /// <summary>The 16 points of the compass, clockwise from north (the lang keys are "compass-" plus these).</summary>
    public static readonly string[] CompassPoints =
        ["N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"];

    /// <summary>The compass point (one of <see cref="CompassPoints"/>) a statue at this angle looks towards.</summary>
    public static string PointOf(float angle)
    {
        // The statue looks along (-cos, sin) in (east, south); a bearing counts clockwise from north.
        var bearing = Math.Atan2(-Math.Cos(angle), -Math.Sin(angle)) * 180 / Math.PI;
        var index = (int)Math.Round((bearing + 360) % 360 / 22.5) % CompassPoints.Length;
        return CompassPoints[index];
    }

    /// <summary>Whether two angles are the same direction (angles repeat every full turn).</summary>
    public static bool Same(float a, float b) =>
        Math.Abs(Math.Sin((a - b) / 2)) < 0.01;

    /// <summary>One press of a turn button: 22.5 degrees.</summary>
    public const float TurnStep = GameMath.PIHALF / 4;

    /// <summary>
    /// The angle after turning to the right (clockwise seen from above) by some steps, or to the left if negative.
    /// Angles run the other way round the compass, so a right turn lowers the angle.
    /// </summary>
    public static float TurnedRight(float angle, int steps) => (angle - steps * TurnStep) % GameMath.TWOPI;

    /// <summary>The angle that makes a statue look at someone who is (dx, dz) away from it.</summary>
    public static float Toward(double dx, double dz) => (float)Math.Atan2(dz, -dx);
}
