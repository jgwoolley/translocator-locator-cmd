using Xunit;

namespace Nf3t.VintageStory.Statue.Tests;

public class StatuePlatformTests
{
    [Fact]
    public void FootprintCoversTheFeetAndIgnoresWhatReachesOutHigherUp()
    {
        // x, y, z for: two hooves on the floor, a back at mid height, and antlers high up and far out to the side.
        float[] xyz =
        [
            0.2f, 0f, 0.3f,
            0.8f, 0.05f, 0.7f,
            0.5f, 0.5f, 0.5f,
            -1f, 1f, 0.5f,
        ];

        var feet = StatuePlatform.Footprint(xyz, 4)!.Value;

        Assert.Equal(0.2f, feet.MinX);
        Assert.Equal(0.8f, feet.MaxX);
        Assert.Equal(0.3f, feet.MinZ);
        Assert.Equal(0.7f, feet.MaxZ);
    }

    [Fact]
    public void AnEmptyMeshHasNoFootprint()
    {
        Assert.Null(StatuePlatform.Footprint([], 0));
    }
}
