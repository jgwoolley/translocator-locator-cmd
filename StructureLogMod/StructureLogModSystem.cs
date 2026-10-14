#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Nf3t.VintageStory.StructureLog;

public class StructureLogModSystem : ModSystem
{
    private readonly HashSet<(string, int, int, int)> _seen = new();
    private ICoreServerAPI? _api;

    // Read on worldgen threads and replaced wholesale by commands, so never mutated in place.
    private volatile StructureLogConfig _config = new();

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        _api = api;
        _config = LoadConfig(api);
        RegisterCommands(api);
        // PreDone runs after the structure passes, so the region's structure list is up to date.
        api.Event.ChunkColumnGeneration(OnChunkColumnGeneration, EnumWorldGenPass.PreDone, "standard");
    }

    private void OnChunkColumnGeneration(IChunkColumnGenerateRequest request)
    {
        var region = request.Chunks[0].MapChunk?.MapRegion;
        if (region == null) return;

        var size = GlobalConstants.ChunkSize;
        var minX = request.ChunkX * size;
        var minZ = request.ChunkZ * size;
        var maxX = minX + size - 1;
        var maxZ = minZ + size - 1;

        GeneratedStructure[] structures;
        lock (region.GeneratedStructures) structures = region.GeneratedStructures.ToArray();

        foreach (var s in structures)
        {
            var loc = s.Location;
            if (loc == null) continue;
            // Only consider structures touching this column; avoids dumping old structures of existing worlds.
            if (loc.X2 < minX || loc.X1 > maxX || loc.Z2 < minZ || loc.Z1 > maxZ) continue;

            bool isNew;
            lock (_seen) isNew = _seen.Add((s.Code ?? "", loc.X1, loc.Y1, loc.Z1));
            if (!isNew) continue;
            if (!_config.ShouldLog(s.Code, s.Group)) continue;

            _api?.Logger.Notification(
                "[StructureLog] Generated structure '{0}' (group: {1}) at ({2}, {3}, {4}) to ({5}, {6}, {7})",
                s.Code, s.Group ?? "-", loc.X1, loc.Y1, loc.Z1, loc.X2, loc.Y2, loc.Z2);
        }
    }

    private static StructureLogConfig LoadConfig(ICoreServerAPI api)
    {
        try
        {
            var config = api.LoadModConfig<StructureLogConfig>(StructureLogConfig.FileName);
            if (config != null) return config;
        }
        catch (Exception e)
        {
            api.Logger.Error("[StructureLog] Failed to load config, using defaults: " + e.Message);
        }

        var defaults = new StructureLogConfig();
        api.StoreModConfig(defaults, StructureLogConfig.FileName);
        return defaults;
    }

    private void Update(Action<StructureLogConfig> change)
    {
        // Copy, change, swap: worldgen threads keep reading a consistent snapshot.
        var next = new StructureLogConfig { Mode = _config.Mode, Patterns = _config.Patterns.ToList() };
        change(next);
        _config = next;
        _api?.StoreModConfig(next, StructureLogConfig.FileName);
    }

    private string Describe() =>
        $"Mode: {_config.Mode.ToString().ToLowerInvariant()}, patterns ({_config.Patterns.Count}): " +
        (_config.Patterns.Count == 0 ? "(none)" : string.Join(", ", _config.Patterns));

    private void RegisterCommands(ICoreServerAPI api)
    {
        var parsers = api.ChatCommands.Parsers;

        api.ChatCommands.Create("structurelog")
            .WithDescription(
                "Filter which generated structures are logged. Patterns are wildcards matched against the whole structure code (e.g. s-kitchen1.json/undergroundruin) and group, so use e.g. *ruin*.")
            .RequiresPrivilege(Privilege.controlserver)
            .BeginSubCommand("list")
                .WithDescription("Show the current mode and patterns")
                .HandleWith(_ => TextCommandResult.Success(Describe()))
            .EndSubCommand()
            .BeginSubCommand("mode")
                .WithDescription("allowlist: log only matching structures. denylist: log everything except matching ones.")
                .WithArgs(parsers.WordRange("mode", "allowlist", "denylist"))
                .HandleWith(args =>
                {
                    var mode = (string)args[0] == "allowlist" ? FilterMode.Allowlist : FilterMode.Denylist;
                    Update(c => c.Mode = mode);
                    return TextCommandResult.Success(Describe());
                })
            .EndSubCommand()
            .BeginSubCommand("add")
                .WithDescription("Add patterns (separated by spaces or commas) to the list")
                .WithArgs(parsers.All("patterns"))
                .HandleWith(args =>
                {
                    var patterns = StructureLogConfig.ParsePatterns((string)args[0]);
                    Update(c => c.Patterns = c.Patterns.Union(patterns).ToList());
                    return TextCommandResult.Success(Describe());
                })
            .EndSubCommand()
            .BeginSubCommand("set")
                .WithDescription("Replace the whole list with the given patterns (same as clear, then add)")
                .WithArgs(parsers.All("patterns"))
                .HandleWith(args =>
                {
                    var patterns = StructureLogConfig.ParsePatterns((string)args[0]);
                    Update(c => c.Patterns = patterns);
                    return TextCommandResult.Success(Describe());
                })
            .EndSubCommand()
            .BeginSubCommand("remove")
                .WithDescription("Remove patterns from the list")
                .WithArgs(parsers.All("patterns"))
                .HandleWith(args =>
                {
                    var patterns = StructureLogConfig.ParsePatterns((string)args[0]);
                    Update(c => c.Patterns = c.Patterns.Except(patterns).ToList());
                    return TextCommandResult.Success(Describe());
                })
            .EndSubCommand()
            .BeginSubCommand("clear")
                .WithDescription("Remove all patterns")
                .HandleWith(_ =>
                {
                    Update(c => c.Patterns = new List<string>());
                    return TextCommandResult.Success(Describe());
                })
            .EndSubCommand();
    }
}
