#nullable enable

using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace Nf3t.VintageStory.EntropyRift;

/// <summary>
/// Reads the vanilla temporal storm schedule (<see cref="SystemTemporalStability.StormData"/>). The server sends that data to
/// every client on join and whenever a storm starts or ends, so this works on both sides.
/// </summary>
public static class StormForecast
{
    /// <summary>Whether this world has temporal storms at all (world settings "Temporal stability" and "Temporal storms").</summary>
    public static bool StormsEnabled(IWorldAccessor world) =>
        world.Config.GetBool("temporalStability", true) && world.Config.GetString("temporalStorms", "") != "off";

    public static bool StormActive(ICoreAPI api) =>
        StormsEnabled(api.World) && api.ModLoader.GetModSystem<SystemTemporalStability>()?.StormData.nowStormActive == true;

    /// <summary>One line describing the next (or current) temporal storm, in <paramref name="langCode"/> (default: this side's language).</summary>
    public static string Describe(ICoreAPI api, string? langCode = null)
    {
        string L(string key, params object[] args) =>
            langCode == null ? Lang.Get("nf3tentropyrift:" + key, args) : Lang.GetL(langCode, "nf3tentropyrift:" + key, args);

        var data = api.ModLoader.GetModSystem<SystemTemporalStability>()?.StormData;
        if (data == null || !StormsEnabled(api.World)) return L("forecast-disabled");

        var now = api.World.Calendar.TotalDays;
        if (data.nowStormActive)
        {
            var hoursLeft = Math.Max(0, (data.stormActiveTotalDays - now) * api.World.Calendar.HoursPerDay);
            return L("forecast-active", hoursLeft);
        }

        var daysLeft = data.nextStormTotalDays - now;
        // The storm system has stopped scheduling (e.g. a playstyle without storms): the "next" storm stays in the past.
        if (daysLeft < -1) return L("forecast-disabled");
        if (daysLeft < 1)
            return L("forecast-hours", Math.Max(0, daysLeft * api.World.Calendar.HoursPerDay));
        return L("forecast-days", daysLeft);
    }
}
