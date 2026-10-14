#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Vintagestory.API.Util;

namespace Nf3t.VintageStory.StructureLog;

public enum FilterMode
{
    /// <summary>Log everything except structures matching a pattern.</summary>
    Denylist,

    /// <summary>Log only structures matching a pattern.</summary>
    Allowlist
}

/// <summary>
/// Stored as ModConfig/nf3tstructurelog.json. Patterns are wildcards (<c>*</c>) matched, case-insensitively,
/// against the structure's code and its group. Codes look like "s-kitchen1.json/undergroundruin", so use e.g. "*ruin*".
/// </summary>
public class StructureLogConfig
{
    public const string FileName = "nf3tstructurelog.json";

    [JsonConverter(typeof(StringEnumConverter))]
    public FilterMode Mode { get; set; } = FilterMode.Denylist;
    public List<string> Patterns { get; set; } = new();

    public bool ShouldLog(string? code, string? group)
    {
        // An empty denylist logs everything; an empty allowlist logs nothing.
        var matches = Patterns.Any(p => Matches(p, code) || Matches(p, group));
        return Mode == FilterMode.Allowlist ? matches : !matches;
    }

    private static bool Matches(string pattern, string? value) =>
        !string.IsNullOrEmpty(value) && WildcardUtil.Match(pattern.ToLowerInvariant(), value.ToLowerInvariant());

    public static string Normalize(string pattern) => pattern.Trim().ToLowerInvariant();

    /// <summary>Splits "a, b  c" into separate patterns.</summary>
    public static List<string> ParsePatterns(string text) =>
        text.Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(Normalize)
            .Distinct()
            .ToList();
}
