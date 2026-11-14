#nullable enable

using System;

namespace Nf3t.VintageStory.Screenshots;

/// <summary>Whether an installed Translocator Navigator understands the route event (added in 2.2.0).</summary>
public static class NavigatorCompatibility
{
    public static readonly Version RouteEventSince = new(2, 2, 0);

    public static bool SupportsRouteEvent(string? version)
    {
        return Version.TryParse(version, out var parsed) && parsed >= RouteEventSince;
    }
}
