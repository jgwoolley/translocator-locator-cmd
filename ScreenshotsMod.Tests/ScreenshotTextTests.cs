using Xunit;

namespace Nf3t.VintageStory.Screenshots.Tests;

public class ScreenshotTextTests
{
    private static ScreenshotRecord Shot(bool hidden = false)
    {
        return new ScreenshotRecord
        {
            File = "2026-11-11_14-02-33.png", X = 512120.5, Y = 72.9, Z = 511955.1, SpawnX = 512000, SpawnZ = 512000,
            GameDate = "4. May, Year 2, 10:00", Label = "Lake house", CoordinatesHidden = hidden,
        };
    }

    [Fact]
    public void Describe_shows_spawn_relative_position_distance_and_direction()
    {
        var text = ScreenshotText.Describe(Shot(), 3, 512000, 512000, worldHidesCoordinates: false);

        Assert.Equal("#3 2026-11-11_14-02-33.png \"Lake house\" at 120, 72, -45 (128 m E), 4. May, Year 2, 10:00", text);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Describe_leaves_out_hidden_positions(bool hiddenWhenTaken, bool hiddenNow)
    {
        var text = ScreenshotText.Describe(Shot(hiddenWhenTaken), 1, 512000, 512000, hiddenNow);

        Assert.Equal("#1 2026-11-11_14-02-33.png \"Lake house\", 4. May, Year 2, 10:00", text);
    }

    [Theory]
    [InlineData(0, -100, "N")]
    [InlineData(100, 0, "E")]
    [InlineData(0, 100, "S")]
    [InlineData(-100, 0, "W")]
    [InlineData(100, -100, "NE")]
    [InlineData(-100, 100, "SW")]
    [InlineData(0, 0, "here")]
    public void Compass_has_north_towards_negative_z(double dx, double dz, string expected)
    {
        Assert.Equal(expected, ScreenshotText.Compass(10, 10, 10 + dx, 10 + dz));
    }
}
