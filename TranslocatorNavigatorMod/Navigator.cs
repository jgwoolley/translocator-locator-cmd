#nullable enable

using System;
using System.Collections.Generic;
using Nf3t.VintageStory.Common;

namespace Nf3t.VintageStory.TranslocatorShortestPath;

/// <summary>The route shown in the dialog and the guidance followed by the HUD, shared between both.</summary>
public class Navigator(Context context)
{
    public Context Context { get; } = context;

    public TranslocatorPathResult? Result { get; private set; }
    public SimplePos Start { get; private set; }
    public SimplePos Goal { get; private set; }
    public IReadOnlyList<RouteStep> Steps { get; private set; } = [];

    /// <summary>Set while the HUD is guiding the player along <see cref="Steps"/>.</summary>
    public RouteProgress? Guidance { get; private set; }

    /// <summary>Raised when the route or the guidance step changes.</summary>
    public event Action? Changed;

    public void FindRoute(SimplePos start, SimplePos goal)
    {
        Start = start;
        Goal = goal;
        Result = Context.CalculatePath(start, goal, refresh: true);
        Steps = Result.IsFounded() ? RouteSteps.Build(Result.Path, Context.KnownTranslocators) : [];
        Guidance = null;
        Changed?.Invoke();
    }

    public void StartGuidance()
    {
        if (Steps.Count == 0) return;
        Guidance = new RouteProgress(Steps);
        Guidance.Update(Context.GetPlayerPos());
        Changed?.Invoke();
    }

    public void StopGuidance()
    {
        if (Guidance == null) return;
        Guidance = null;
        Changed?.Invoke();
    }

    public void Tick()
    {
        if (Guidance != null && Guidance.Update(Context.GetPlayerPos())) Changed?.Invoke();
    }

    public bool IsTranslocator(SimplePos pos)
    {
        return Context.KnownTranslocators.ContainsKey(pos);
    }

    public string Coords(SimplePos pos)
    {
        return pos.ToRelativeString(Context.DefaultSpawnPosition);
    }

    /// <summary>A step as listed in the dialog, e.g. "Walk 340 m NE to translocator at 120, 72, -45".</summary>
    public string Describe(RouteStep step)
    {
        if (step.Kind == StepKind.Teleport) return $"Teleport to {Coords(step.To)}";
        var what = IsTranslocator(step.To) ? "translocator at " : "";
        return $"Walk {step.Distance} m {step.To.GetCompassDirection(step.From)} to {what}{Coords(step.To)}";
    }

    /// <summary>The current step as shown on the HUD, measured from where the player is now.</summary>
    public string DescribeLive(RouteStep step, SimplePos player)
    {
        if (step.Kind == StepKind.Teleport)
            return $"Use the translocator at {Coords(step.From)} ({(int)step.From.DistanceTo(player)} m), exits at {Coords(step.To)}";
        var what = IsTranslocator(step.To) ? "translocator at " : "";
        return $"Walk {(int)step.To.DistanceTo(player)} m {step.To.GetCompassDirection(player)} to {what}{Coords(step.To)}";
    }
}
