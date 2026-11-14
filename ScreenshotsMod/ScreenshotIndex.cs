#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Nf3t.VintageStory.Screenshots;

/// <summary>All recorded screenshots across worlds and servers, kept in one JSON file next to the game's other mod
/// data (not in the image files, so a shared screenshot doesn't give away where it was taken).</summary>
public class ScreenshotIndex
{
    private readonly List<ScreenshotRecord> records = [];

    public IReadOnlyList<ScreenshotRecord> All => records;

    public static ScreenshotIndex Load(string path)
    {
        var index = new ScreenshotIndex();
        if (!File.Exists(path)) return index;
        var loaded = JsonConvert.DeserializeObject<List<ScreenshotRecord>>(File.ReadAllText(path));
        if (loaded != null) index.records.AddRange(loaded.Where(r => !string.IsNullOrEmpty(r.File)));
        return index;
    }

    public void Save(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        // Write-then-move so a crash can't leave a half-written index.
        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, JsonConvert.SerializeObject(records, Formatting.Indented));
        File.Move(tempPath, path, true);
    }

    public bool Contains(string file)
    {
        return records.Exists(r => string.Equals(r.File, file, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Adds a screenshot, replacing an older record of the same file (the game names files by the second, so
    /// two in one second overwrite each other on disk too).</summary>
    public void Add(ScreenshotRecord record)
    {
        records.RemoveAll(r => string.Equals(r.File, record.File, StringComparison.OrdinalIgnoreCase));
        records.Add(record);
    }

    /// <summary>The screenshots of one world, newest first. Their position in this list (from 1) is the number
    /// the commands use.</summary>
    public IReadOnlyList<ScreenshotRecord> ForWorld(string savegameId)
    {
        return records.Where(r => r.SavegameId == savegameId)
            .OrderByDescending(r => r.TakenUtc)
            .ToList();
    }

    /// <summary>Screenshots of one world within <paramref name="radius"/> blocks (horizontally), nearest first.
    /// Leaves out those whose position is hidden.</summary>
    public IReadOnlyList<ScreenshotRecord> Near(string savegameId, double x, double z, double radius)
    {
        return ForWorld(savegameId)
            .Where(r => !r.CoordinatesHidden && r.HorizontalDistanceTo(x, z) <= radius)
            .OrderBy(r => r.HorizontalDistanceTo(x, z))
            .ToList();
    }

    /// <summary>Finds a screenshot of one world by its number in <see cref="ForWorld"/> or by file name (with or
    /// without <c>.png</c>).</summary>
    public ScreenshotRecord? Find(string savegameId, string key)
    {
        key = key.Trim();
        var world = ForWorld(savegameId);
        if (int.TryParse(key, out var number))
            return number >= 1 && number <= world.Count ? world[number - 1] : null;
        return world.FirstOrDefault(r =>
            string.Equals(r.File, key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileNameWithoutExtension(r.File), key, StringComparison.OrdinalIgnoreCase));
    }

    public int NumberOf(ScreenshotRecord record)
    {
        var world = ForWorld(record.SavegameId);
        for (var i = 0; i < world.Count; i++)
            if (ReferenceEquals(world[i], record))
                return i + 1;
        return 0;
    }

    /// <summary>How many screenshots each world has, labelled by server or world name, most first.</summary>
    public IReadOnlyList<(string Name, string SavegameId, int Count)> Worlds()
    {
        return records.GroupBy(r => r.SavegameId)
            .Select(g =>
            {
                var newest = g.OrderByDescending(r => r.TakenUtc).First();
                if (g.Key == ScreenshotImport.UnknownWorld) return ("Unknown world (imported)", g.Key, g.Count());
                var name = newest.ServerName;
                if (string.IsNullOrWhiteSpace(name)) name = newest.ServerAddress ?? "Singleplayer world";
                return (name!, g.Key, g.Count());
            })
            .OrderByDescending(w => w.Item3)
            .ToList();
    }
}
