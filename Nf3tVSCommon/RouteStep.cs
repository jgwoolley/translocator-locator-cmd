namespace Nf3t.VintageStory.Common;

public enum StepKind
{
    Walk,
    Teleport
}

/// <summary>One leg of a route: walk from <see cref="From"/> to <see cref="To"/>, or step through the translocator at
/// <see cref="From"/> and come out at <see cref="To"/>.</summary>
public readonly record struct RouteStep(StepKind Kind, SimplePos From, SimplePos To, int Distance);

public static class RouteSteps
{
    /// <summary>Turns the nodes of a path into legs. A pair of nodes is a teleport when either one is a recorded
    /// translocator leading to the other; anything else is walked.</summary>
    public static IReadOnlyList<RouteStep> Build(IReadOnlyList<SimplePos> path,
        IReadOnlyDictionary<SimplePos, SimplePos?> translocators)
    {
        var steps = new List<RouteStep>();
        for (var i = 1; i < path.Count; i++)
        {
            var from = path[i - 1];
            var to = path[i];
            if (from == to) continue;
            var teleport = (translocators.TryGetValue(from, out var a) && a == to) ||
                           (translocators.TryGetValue(to, out var b) && b == from);
            steps.Add(teleport
                ? new RouteStep(StepKind.Teleport, from, to, 0)
                : new RouteStep(StepKind.Walk, from, to, (int)from.DistanceTo(to)));
        }

        return steps;
    }

    /// <summary>Converts coordinates as the game shows them (X and Z relative to spawn) to world coordinates.</summary>
    public static SimplePos FromRelative(int x, int y, int z, SimplePos defaultSpawnPosition)
    {
        return new SimplePos(x + defaultSpawnPosition.X, y, z + defaultSpawnPosition.Z);
    }
}

/// <summary>Follows the player along a route. Only ever moves forward.</summary>
public class RouteProgress
{
    public const double ArrivalRadius = 5;

    public RouteProgress(IReadOnlyList<RouteStep> steps)
    {
        Steps = steps;
    }

    public IReadOnlyList<RouteStep> Steps { get; }

    /// <summary>The step being worked on; equals <c>Steps.Count</c> once arrived.</summary>
    public int Index { get; private set; }

    public bool IsArrived => Index >= Steps.Count;
    public RouteStep? Current => IsArrived ? null : Steps[Index];

    /// <returns>True if the current step changed.</returns>
    public bool Update(SimplePos player)
    {
        // The furthest step whose end the player is standing at; covers coming out of a translocator exit.
        for (var i = Steps.Count - 1; i >= Index; i--)
        {
            if (Steps[i].To.DistanceTo(player) > ArrivalRadius) continue;
            Index = i + 1;
            return true;
        }

        return false;
    }
}
