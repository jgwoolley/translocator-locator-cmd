using Xunit;

namespace Nf3t.VintageStory.Screenshots.Tests;

public class ScreenshotImportTests
{
    private const string Json =
        "{\"Type\":\"screenshotLocationMetaData\",\"Pos\":{\"X\":512120.5,\"Y\":72.9,\"Z\":511955.1}," +
        "\"RollYawPitch\":{\"X\":0.0,\"Y\":1.25,\"Z\":-0.5},\"WorldSeed\":1234,\"WorldConfig\":\"{}\"}";

    private static readonly DateTime FileTime = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Same_seed_is_put_in_the_current_world_with_its_spawn()
    {
        var record = ScreenshotImport.FromVanillaExif("2026-11-11_14-02-33.png", FileTime, Json, "world-a", 1234, 512000, 512000);

        Assert.NotNull(record);
        Assert.Equal("world-a", record!.SavegameId);
        Assert.Equal(ScreenshotImport.VanillaSource, record.Source);
        Assert.Equal((120, 72, -45), (record.RelX, record.RelY, record.RelZ));
        Assert.Equal(1.25f, record.Yaw);
        Assert.Equal(-0.5f, record.Pitch);
        Assert.Equal(1234, record.Seed);
    }

    [Fact]
    public void Other_seed_is_marked_unknown_with_absolute_position()
    {
        var record = ScreenshotImport.FromVanillaExif("2026-11-11_14-02-33.png", FileTime, Json, "world-a", 999, 512000, 512000);

        Assert.NotNull(record);
        Assert.Equal(ScreenshotImport.UnknownWorld, record!.SavegameId);
        Assert.Equal(ScreenshotImport.VanillaSource, record.Source);
        Assert.Equal((512120, 72, 511955), (record.RelX, record.RelY, record.RelZ));
    }

    [Fact]
    public void No_description_or_other_metadata_gives_null()
    {
        Assert.Null(ScreenshotImport.FromVanillaExif("a.png", FileTime, null, "w", 1, 0, 0));
        Assert.Null(ScreenshotImport.FromVanillaExif("a.png", FileTime, "Nf3t notes", "w", 1, 0, 0));
        Assert.Null(ScreenshotImport.FromVanillaExif("a.png", FileTime, "{\"Type\":\"something else\"}", "w", 1, 0, 0));
        Assert.Null(ScreenshotImport.FromVanillaExif("a.png", FileTime, "{\"Type\":\"screenshotLocationMetaData\"}", "w", 1, 0, 0));
    }

    [Fact]
    public void Time_comes_from_the_file_name_in_local_time()
    {
        var expected = new DateTime(2026, 11, 11, 14, 2, 33, DateTimeKind.Local).ToUniversalTime();

        Assert.Equal(expected, ScreenshotImport.TakenFromFileName("2026-11-11_14-02-33.png"));
        Assert.Null(ScreenshotImport.TakenFromFileName("holiday.png"));
    }
}
