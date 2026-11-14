#nullable enable

using System;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Nf3t.VintageStory.Screenshots;

/// <summary>Reads where the player is and which world or server they are on, at the moment a screenshot is taken.</summary>
public static class ScreenshotCapture
{
    public static bool WorldHidesCoordinates(ICoreClientAPI api)
    {
        return !api.World.Config.GetBool("allowCoordinateHud", true);
    }

    public static ScreenshotRecord Capture(ICoreClientAPI api)
    {
        var entity = api.World.Player.Entity;
        var pos = entity.Pos;
        var spawn = api.World.DefaultSpawnPosition.AsBlockPos;
        var blockPos = pos.AsBlockPos;

        var record = new ScreenshotRecord
        {
            TakenUtc = DateTime.UtcNow,
            SavegameId = api.World.SavegameIdentifier,
            Seed = api.World.Seed,
            ServerName = ReadServerName(api),
            ServerAddress = api.IsSinglePlayer ? null : ReadServerAddress(api),
            X = pos.X,
            Y = pos.Y,
            Z = pos.Z,
            Yaw = pos.Yaw,
            Pitch = pos.Pitch,
            SpawnX = spawn.X,
            SpawnZ = spawn.Z,
            CoordinatesHidden = WorldHidesCoordinates(api),
        };

        // The extras are nice to have; a failure in one must not lose the screenshot's record.
        try
        {
            record.GameDate = api.World.Calendar.PrettyDate();
            record.Season = api.World.Calendar.GetSeason(blockPos).ToString();
            var climate = api.World.BlockAccessor.GetClimateAt(blockPos);
            if (climate != null)
            {
                record.Temperature = climate.Temperature;
                record.Rainfall = climate.Rainfall;
            }
        }
        catch (Exception e)
        {
            api.Logger.Warning("[Screenshots] Could not read date or climate: {0}", e.Message);
        }

        return record;
    }

    /// <summary><c>host:port</c> from the game's connection data (public field on an internal class). Only the host and
    /// port are copied: the same object holds the server password and the login token.</summary>
    private static string? ReadServerAddress(ICoreClientAPI api)
    {
        try
        {
            var connectData = api.World.GetType().GetField("Connectdata", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(api.World);
            if (connectData == null) return null;
            var type = connectData.GetType();
            var host = type.GetField("Host")?.GetValue(connectData) as string;
            var port = type.GetField("Port")?.GetValue(connectData) as int?;
            if (string.IsNullOrWhiteSpace(host)) return null;
            return port is > 0 ? $"{host}:{port}" : host;
        }
        catch (Exception e)
        {
            api.Logger.Warning("[Screenshots] Could not read the server address: {0}", e.Message);
            return null;
        }
    }

    /// <summary>The name the server announced (internal <c>ClientMain.ServerInfo.ServerName</c>).</summary>
    private static string? ReadServerName(ICoreClientAPI api)
    {
        try
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var info = api.World.GetType().GetField("ServerInfo", flags)?.GetValue(api.World);
            var name = info?.GetType().GetField("ServerName", flags)?.GetValue(info) as string;
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
        catch (Exception e)
        {
            api.Logger.Warning("[Screenshots] Could not read the server name: {0}", e.Message);
            return null;
        }
    }

    /// <summary>The JSON put into the image's EXIF data when <see cref="ScreenshotsConfig.WriteExif"/> is on. Keeps the
    /// field names of the game's own screenshot metadata (<c>Pos</c>, <c>RollYawPitch</c>, <c>WorldSeed</c>) so tools
    /// that read those still work.</summary>
    public static string ExifJson(ScreenshotRecord record)
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            Type = "screenshotLocationMetaData",
            Pos = new { X = record.X, Y = record.Y, Z = record.Z },
            RollYawPitch = new { X = 0f, Y = record.Yaw, Z = record.Pitch },
            WorldSeed = record.Seed,
            Nf3tScreenshots = new
            {
                record.SavegameId,
                record.ServerName,
                record.ServerAddress,
                Relative = new { X = record.RelX, Y = record.RelY, Z = record.RelZ },
                record.GameDate,
                record.Season,
                record.Temperature,
                record.Rainfall,
            },
        });
    }

    public static Vec3d PlayerPos(ICoreClientAPI api)
    {
        return api.World.Player.Entity.Pos.XYZ;
    }
}
