namespace Nf3t.VintageStory.Common;

/// <summary>Directions on the ground plane, with the game's axes: x is east, z is south.</summary>
public static class Bearing
{
    /// <summary>
    /// The turn from the direction the player looks, (<paramref name="fx"/>, <paramref name="fz"/>), to the target
    /// offset (<paramref name="dx"/>, <paramref name="dz"/>), in radians, clockwise seen from above: 0 is straight
    /// ahead, positive is to the right, ±π is behind. Null when either vector is (nearly) zero, e.g. when looking
    /// straight down or standing on the target.
    /// </summary>
    public static double? Relative(double fx, double fz, double dx, double dz)
    {
        if (fx * fx + fz * fz < 1e-6 || dx * dx + dz * dz < 1e-6) return null;
        // Seen from above with north up, the right of (fx, fz) is (-fz, fx): looking north (0, -1), right is east (1, 0).
        var ahead = dx * fx + dz * fz;
        var right = dx * -fz + dz * fx;
        return Math.Atan2(right, ahead);
    }
}
