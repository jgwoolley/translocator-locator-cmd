#nullable enable

using System;

namespace Nf3t.VintageStory.Screenshots;

/// <summary>Where and when one screenshot was taken. Never holds the player's name, the server password or the
/// login token.</summary>
public class ScreenshotRecord
{
    /// <summary>File name in the game's screenshots folder, e.g. <c>2026-11-11_14-02-33.png</c>.</summary>
    public string File { get; set; } = "";

    public DateTime TakenUtc { get; set; }

    /// <summary>The world's savegame identifier: the key that ties a screenshot to a world, even when the server
    /// moves to another address.</summary>
    public string SavegameId { get; set; } = "";

    /// <summary>The server's name as it announces itself (in singleplayer, what the game reports for the world).</summary>
    public string? ServerName { get; set; }

    /// <summary><c>host:port</c> of a multiplayer server; null in singleplayer.</summary>
    public string? ServerAddress { get; set; }

    public int Seed { get; set; }

    /// <summary>Absolute world position of the player.</summary>
    public double X { get; set; }

    public double Y { get; set; }
    public double Z { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }

    /// <summary>The world's default spawn, so positions can be shown as the game shows them (X and Z relative to spawn).</summary>
    public int SpawnX { get; set; }

    public int SpawnZ { get; set; }

    /// <summary>The world had the coordinates HUD turned off when this was taken. Such positions are kept but never
    /// shown, measured or navigated to.</summary>
    public bool CoordinatesHidden { get; set; }

    /// <summary>In-game date and time, as the game writes it.</summary>
    public string? GameDate { get; set; }

    public string? Season { get; set; }
    public float? Temperature { get; set; }
    public float? Rainfall { get; set; }
    public bool Mega { get; set; }

    /// <summary>Null for screenshots taken while this mod was running; <see cref="ScreenshotImport.VanillaSource"/> for
    /// ones imported from the game's own EXIF data by <c>.shots refresh</c>.</summary>
    public string? Source { get; set; }

    /// <summary>Set by the player with <c>.shots label</c>.</summary>
    public string? Label { get; set; }

    public int RelX => (int)Math.Floor(X) - SpawnX;
    public int RelY => (int)Math.Floor(Y);
    public int RelZ => (int)Math.Floor(Z) - SpawnZ;

    public double HorizontalDistanceTo(double x, double z)
    {
        var dx = X - x;
        var dz = Z - z;
        return Math.Sqrt(dx * dx + dz * dz);
    }
}
