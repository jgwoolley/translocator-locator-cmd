#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Nf3t.VintageStory.SchematicLocator;

/// <summary>
/// Server commands over the structures worldgen has already placed. The game records every one it generates in its map
/// region (<see cref="IMapRegion.GeneratedStructures"/>: a box and a code such as <c>s-kitchen1.json/undergroundruin</c>), and
/// saves that with the world; these commands read it. Only regions loaded now (near players) are searched, and land nobody
/// has visited has no structures yet.
/// <list type="bullet">
/// <item><c>/liststructures [radius]</c>: every kind of structure within the radius, how many and the nearest.</item>
/// <item><c>/findstructure &lt;filter&gt; [radius]</c>: the nearest structures whose code matches the filter (part of the code,
/// or a pattern with <c>*</c>), with their positions and a <c>/tp</c> command.</item>
/// </list>
/// Admin only (<c>controlserver</c>): on a shared server they give away every ruin.
/// </summary>
public static class StructureCommands
{
    private const int DefaultRadius = 1000;
    private const int MaxListed = 25;

    public static void Register(ICoreServerAPI api)
    {
        var parsers = api.ChatCommands.Parsers;
        api.ChatCommands.Create("liststructures")
            .WithDescription("Lists the kinds of generated structures near you (in loaded map regions): how many, and the nearest.")
            .RequiresPrivilege(Privilege.controlserver)
            .WithArgs(parsers.OptionalInt("radius", DefaultRadius))
            .HandleWith(args => List(api, args.Caller, (int)args[0]));
        api.ChatCommands.Create("findstructure")
            .WithDescription("Finds the nearest generated structures whose code matches a filter (part of the code, or a pattern with *), e.g. /findstructure shrine")
            .RequiresPrivilege(Privilege.controlserver)
            .WithArgs(parsers.Word("filter"), parsers.OptionalInt("radius", DefaultRadius))
            .HandleWith(args => Find(api, args.Caller, (string)args[0], (int)args[1]));
    }

    private readonly record struct Found(GeneratedStructure Structure, double Distance, double Dx, double Dz);

    /// <summary>Generated structures in loaded regions within <paramref name="radius"/> blocks (horizontally) of
    /// <paramref name="from"/>, nearest first.</summary>
    private static List<Found> Near(ICoreServerAPI api, Vec3d from, int radius, out int regions)
    {
        var found = new List<Found>();
        var loaded = api.WorldManager.AllLoadedMapRegions.Values.ToArray();
        regions = loaded.Length;
        foreach (var region in loaded)
        {
            GeneratedStructure[] structures;
            lock (region.GeneratedStructures) structures = region.GeneratedStructures.ToArray();
            foreach (var s in structures)
            {
                var loc = s.Location;
                double dx = (loc.X1 + loc.X2) / 2.0 - from.X, dz = (loc.Z1 + loc.Z2) / 2.0 - from.Z;
                var distance = Math.Sqrt(dx * dx + dz * dz);
                if (distance <= radius) found.Add(new Found(s, distance, dx, dz));
            }
        }
        return found.OrderBy(f => f.Distance).ToList();
    }

    /// <summary>The structure part of a code: <c>s-kitchen1.json/undergroundruin</c> is <c>undergroundruin</c> (a structure
    /// with several schematics shows the file it picked before the slash).</summary>
    private static string Kind(string? code)
    {
        if (string.IsNullOrEmpty(code)) return "?";
        var slash = code.LastIndexOf('/');
        return slash >= 0 ? code[(slash + 1)..] : code;
    }

    private static bool Matches(string filter, GeneratedStructure s)
    {
        var code = s.Code ?? "";
        if (filter.Contains('*')) return WildcardUtil.Match(filter, code) || (s.Group != null && WildcardUtil.Match(filter, s.Group));
        return code.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
               (s.Group?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static Vec3d Origin(ICoreServerAPI api, Caller caller) =>
        caller.Pos ?? api.World.DefaultSpawnPosition.XYZ;

    private static TextCommandResult List(ICoreServerAPI api, Caller caller, int radius)
    {
        var found = Near(api, Origin(api, caller), radius, out var regions);
        if (found.Count == 0)
            return TextCommandResult.Success($"No generated structures within {radius} blocks ({regions} map regions loaded).");

        var sb = new StringBuilder($"{found.Count} structures within {radius} blocks ({regions} map regions loaded):");
        foreach (var kind in found.GroupBy(f => Kind(f.Structure.Code)).OrderBy(g => g.First().Distance))
        {
            var nearest = kind.First();
            sb.Append($"\n{kind.Key}: {kind.Count()}, nearest {nearest.Distance:0} blocks {Direction(nearest.Dx, nearest.Dz)}");
        }
        sb.Append("\nUse /findstructure <name> for positions.");
        return TextCommandResult.Success(sb.ToString());
    }

    private static TextCommandResult Find(ICoreServerAPI api, Caller caller, string filter, int radius)
    {
        var found = Near(api, Origin(api, caller), radius, out var regions).Where(f => Matches(filter, f.Structure)).ToList();
        if (found.Count == 0)
            return TextCommandResult.Success($"No generated structure matching '{filter}' within {radius} blocks ({regions} map regions loaded). " +
                                             "Only explored land has structures; try /liststructures for the names.");

        var spawn = api.World.DefaultSpawnPosition;
        var sb = new StringBuilder($"{found.Count} matching '{filter}' within {radius} blocks" +
                                   (found.Count > MaxListed ? $", the nearest {MaxListed}:" : ":"));
        foreach (var f in found.Take(MaxListed))
        {
            var loc = f.Structure.Location;
            int x = (loc.X1 + loc.X2) / 2, z = (loc.Z1 + loc.Z2) / 2, top = loc.Y2 + 1;
            // As the coordinates HUD shows them (relative to the world spawn), and an absolute /tp to above its middle.
            sb.Append($"\n{f.Distance:0} blocks {Direction(f.Dx, f.Dz)}: {f.Structure.Code} at {x - (int)spawn.X}, {top}, {z - (int)spawn.Z}" +
                      $"  (/tp ={x} {top} ={z})");
        }
        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>A compass direction for an offset (x east, z south).</summary>
    private static string Direction(double dx, double dz)
    {
        if (Math.Abs(dx) < 1 && Math.Abs(dz) < 1) return "here";
        string[] names = ["E", "SE", "S", "SW", "W", "NW", "N", "NE"];
        var angle = Math.Atan2(dz, dx);   // 0 east, +PI/2 south
        var index = (int)Math.Round(angle / (Math.PI / 4));
        return names[((index % 8) + 8) % 8];
    }
}
