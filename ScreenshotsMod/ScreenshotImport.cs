#nullable enable

using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Nf3t.VintageStory.Screenshots;

/// <summary>
/// Turns a screenshot the game saved with its own location metadata (<c>screenshotExifDataMode</c>) into a record.
/// That metadata has the position, view direction and world seed only: no savegame ID and no server. So a screenshot
/// is put in the current world when its seed matches, and otherwise in <see cref="UnknownWorld"/>, where its
/// position is absolute (no spawn offset). Records are marked <see cref="VanillaSource"/>.
/// </summary>
public static class ScreenshotImport
{
    /// <summary>The savegame ID used for imported screenshots whose world could not be identified.</summary>
    public const string UnknownWorld = "unknown";

    /// <summary>Marks records imported from the game's own EXIF data rather than taken with this mod running.</summary>
    public const string VanillaSource = "vanilla-exif";

    private const string MetaDataType = "screenshotLocationMetaData";

    /// <summary>Builds the record, or returns null when the file has no game location metadata (such screenshots are
    /// ignored).</summary>
    /// <param name="fileName">The screenshot's file name, e.g. <c>2026-11-11_14-02-33.png</c>; its time is used when it
    /// has the game's format.</param>
    /// <param name="fileWriteTimeUtc">Used when the file name doesn't have a time.</param>
    /// <param name="description">The ImageDescription text from <see cref="ExifReader"/>.</param>
    /// <param name="currentSavegameId">The world the player is in now.</param>
    /// <param name="currentSeed">The seed of that world.</param>
    /// <param name="spawnX">Spawn X of that world, used for positions shown relative to spawn.</param>
    /// <param name="spawnZ">Spawn Z of that world.</param>
    public static ScreenshotRecord? FromVanillaExif(string fileName, DateTime fileWriteTimeUtc, string? description,
        string currentSavegameId, int currentSeed, int spawnX, int spawnZ)
    {
        if (string.IsNullOrWhiteSpace(description)) return null;

        JObject json;
        try
        {
            json = JObject.Parse(description);
        }
        catch (Exception)
        {
            return null;
        }

        if (json["Type"]?.Value<string>() != MetaDataType) return null;
        var pos = json["Pos"];
        var rollYawPitch = json["RollYawPitch"];
        if (pos == null || rollYawPitch == null) return null;

        var seed = json["WorldSeed"]?.Value<int>() ?? 0;
        var matched = seed == currentSeed;

        return new ScreenshotRecord
        {
            File = fileName,
            TakenUtc = TakenFromFileName(fileName) ?? fileWriteTimeUtc,
            SavegameId = matched ? currentSavegameId : UnknownWorld,
            Seed = seed,
            X = pos["X"]?.Value<double>() ?? 0,
            Y = pos["Y"]?.Value<double>() ?? 0,
            Z = pos["Z"]?.Value<double>() ?? 0,
            // The game stores Vec3f(roll, yaw, pitch).
            Yaw = rollYawPitch["Y"]?.Value<float>() ?? 0,
            Pitch = rollYawPitch["Z"]?.Value<float>() ?? 0,
            SpawnX = matched ? spawnX : 0,
            SpawnZ = matched ? spawnZ : 0,
            Source = VanillaSource,
        };
    }

    /// <summary>The game names files <c>yyyy-MM-dd_HH-mm-ss.png</c> in local time.</summary>
    public static DateTime? TakenFromFileName(string fileName)
    {
        var stem = System.IO.Path.GetFileNameWithoutExtension(fileName);
        if (!DateTime.TryParseExact(stem, "yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var local))
            return null;
        return DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();
    }
}
