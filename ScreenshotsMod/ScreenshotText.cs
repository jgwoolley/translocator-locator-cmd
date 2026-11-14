#nullable enable

using System;
using System.Globalization;

namespace Nf3t.VintageStory.Screenshots;

/// <summary>How a screenshot is described in chat. Positions are left out whenever they are hidden, either because
/// the world hid coordinates when the screenshot was taken or because it hides them now.</summary>
public static class ScreenshotText
{
    public static bool IsHidden(ScreenshotRecord record, bool worldHidesCoordinates)
    {
        return worldHidesCoordinates || record.CoordinatesHidden;
    }

    /// <summary>e.g. <c>#3 2026-11-11_14-02-33.png "Lake house" at 120, 72, -45 (340 m NE), 4. May, Year 2, 10:00</c>.</summary>
    public static string Describe(ScreenshotRecord record, int number, double? playerX, double? playerZ,
        bool worldHidesCoordinates)
    {
        var text = $"#{number} {record.File}";
        if (!string.IsNullOrWhiteSpace(record.Label)) text += $" \"{record.Label}\"";
        if (!IsHidden(record, worldHidesCoordinates))
        {
            text += $" at {record.RelX}, {record.RelY}, {record.RelZ}";
            if (playerX != null && playerZ != null)
                text += $" ({(int)record.HorizontalDistanceTo(playerX.Value, playerZ.Value)} m " +
                        $"{Compass(playerX.Value, playerZ.Value, record.X, record.Z)})";
        }

        if (!string.IsNullOrWhiteSpace(record.GameDate)) text += $", {record.GameDate}";
        return text;
    }

    /// <summary>The direction from one point to another, with north towards negative Z as in the game.</summary>
    public static string Compass(double fromX, double fromZ, double toX, double toZ)
    {
        var dx = toX - fromX;
        var dz = toZ - fromZ;
        if (Math.Abs(dx) < 0.5 && Math.Abs(dz) < 0.5) return "here";
        // 0 = east, 90 = south (positive Z), as in Nf3tVSCommon's SimplePos.GetCompassDirection.
        var angle = (Math.Atan2(dz, dx) * 180 / Math.PI + 360) % 360;
        string[] names = ["E", "SE", "S", "SW", "W", "NW", "N", "NE"];
        return names[(int)Math.Round(angle / 45) % 8];
    }

    public static string Climate(ScreenshotRecord record)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrWhiteSpace(record.Season)) parts.Add(record.Season!);
        if (record.Temperature != null)
            parts.Add(record.Temperature.Value.ToString("0", CultureInfo.InvariantCulture) + " °C");
        if (record.Rainfall != null)
            parts.Add("rainfall " + record.Rainfall.Value.ToString("0.00", CultureInfo.InvariantCulture));
        return string.Join(", ", parts);
    }
}
