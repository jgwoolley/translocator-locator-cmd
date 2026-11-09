namespace Nf3t.VintageStory.Statue;

/// <summary>
/// Sizing for the stone slab a mounted statue stands on. The block's own pedestal is far too small for an elk.
/// </summary>
public static class StatuePlatform
{
    /// <summary>How far the slab sticks out past the feet, in model pixels.</summary>
    public const float MarginPixels = 2;

    /// <summary>
    /// The area on the floor covered by the lowest part of a mesh (its feet), as min and max x and z in block units.
    /// Only vertices in the bottom <paramref name="floorFraction"/> of the mesh count, so antlers or a raised arm
    /// reaching out do not widen the slab.
    /// </summary>
    public static (float MinX, float MaxX, float MinZ, float MaxZ)? Footprint(float[] xyz, int vertexCount,
        float floorFraction = 0.25f)
    {
        if (vertexCount == 0) return null;

        float minY = float.MaxValue, maxY = float.MinValue;
        for (var i = 0; i < vertexCount; i++)
        {
            minY = System.Math.Min(minY, xyz[i * 3 + 1]);
            maxY = System.Math.Max(maxY, xyz[i * 3 + 1]);
        }

        var limit = minY + (maxY - minY) * floorFraction;
        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        for (var i = 0; i < vertexCount; i++)
        {
            if (xyz[i * 3 + 1] > limit) continue;
            minX = System.Math.Min(minX, xyz[i * 3]);
            maxX = System.Math.Max(maxX, xyz[i * 3]);
            minZ = System.Math.Min(minZ, xyz[i * 3 + 2]);
            maxZ = System.Math.Max(maxZ, xyz[i * 3 + 2]);
        }

        return (minX, maxX, minZ, maxZ);
    }
}
