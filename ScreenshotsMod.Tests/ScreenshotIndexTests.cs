using Xunit;

namespace Nf3t.VintageStory.Screenshots.Tests;

public class ScreenshotIndexTests
{
    private static ScreenshotRecord Shot(string file, string world, int minutes, double x = 0, double z = 0,
        bool hidden = false)
    {
        return new ScreenshotRecord
        {
            File = file, SavegameId = world, TakenUtc = new DateTime(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc).AddMinutes(minutes),
            X = x, Y = 110, Z = z, SpawnX = 512000, SpawnZ = 512000, CoordinatesHidden = hidden,
        };
    }

    [Fact]
    public void ForWorld_lists_only_that_world_newest_first()
    {
        var index = new ScreenshotIndex();
        index.Add(Shot("a.png", "w1", 0));
        index.Add(Shot("b.png", "w2", 5));
        index.Add(Shot("c.png", "w1", 10));

        Assert.Equal(["c.png", "a.png"], index.ForWorld("w1").Select(r => r.File));
    }

    [Fact]
    public void Adding_the_same_file_replaces_the_old_record()
    {
        var index = new ScreenshotIndex();
        index.Add(Shot("a.png", "w1", 0, x: 1));
        index.Add(Shot("A.png", "w1", 1, x: 2));

        Assert.Equal(2, Assert.Single(index.All).X);
    }

    [Fact]
    public void Find_by_number_or_file_name()
    {
        var index = new ScreenshotIndex();
        index.Add(Shot("2026-11-11_12-00-00.png", "w1", 0));
        index.Add(Shot("2026-11-11_12-10-00.png", "w1", 10));

        Assert.Equal("2026-11-11_12-10-00.png", index.Find("w1", "1")?.File);
        Assert.Equal("2026-11-11_12-00-00.png", index.Find("w1", "2")?.File);
        Assert.Equal("2026-11-11_12-00-00.png", index.Find("w1", "2026-11-11_12-00-00")?.File);
        Assert.Null(index.Find("w1", "3"));
        Assert.Null(index.Find("w1", "0"));
        Assert.Null(index.Find("w2", "1"));
    }

    [Fact]
    public void Near_is_nearest_first_and_skips_hidden_positions()
    {
        var index = new ScreenshotIndex();
        index.Add(Shot("far.png", "w1", 0, x: 400));
        index.Add(Shot("close.png", "w1", 1, x: 50));
        index.Add(Shot("hidden.png", "w1", 2, x: 10, hidden: true));
        index.Add(Shot("outside.png", "w1", 3, x: 600));

        Assert.Equal(["close.png", "far.png"], index.Near("w1", 0, 0, 500).Select(r => r.File));
    }

    [Fact]
    public void Save_and_load_round_trip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nf3tshots-{Guid.NewGuid()}", "index.json");
        try
        {
            var index = new ScreenshotIndex();
            var shot = Shot("a.png", "w1", 0, x: 512100.7, z: 511900.2);
            shot.ServerAddress = "play.example.org:42420";
            shot.Label = "Lake house";
            shot.Temperature = 12.5f;
            index.Add(shot);
            index.Save(path);

            var loaded = Assert.Single(ScreenshotIndex.Load(path).All);
            Assert.Equal("play.example.org:42420", loaded.ServerAddress);
            Assert.Equal("Lake house", loaded.Label);
            Assert.Equal(12.5f, loaded.Temperature);
            Assert.Equal((100, 110, -100), (loaded.RelX, loaded.RelY, loaded.RelZ));
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    [Fact]
    public void Load_of_a_missing_file_is_empty()
    {
        Assert.Empty(ScreenshotIndex.Load(Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid()}.json")).All);
    }

    [Fact]
    public void Worlds_are_named_by_server_then_address()
    {
        var index = new ScreenshotIndex();
        var a = Shot("a.png", "w1", 0);
        a.ServerName = "Gilded";
        var b = Shot("b.png", "w2", 0);
        b.ServerAddress = "1.2.3.4:42420";
        var c = Shot("c.png", "w2", 1);
        c.ServerAddress = "1.2.3.4:42420";
        index.Add(a);
        index.Add(b);
        index.Add(c);

        Assert.Equal([("1.2.3.4:42420", "w2", 2), ("Gilded", "w1", 1)], index.Worlds());
    }
}
