using System.Collections.ObjectModel;
using Dijkstra.NET.Graph;
using Dijkstra.NET.ShortestPath;

namespace Nf3t.VintageStory.Common;

public class TranslocatorPathResult
{
    private readonly SimplePos _goalPos;
    private readonly Graph<SimplePos, string> _graph;
    private readonly ShortestPathResult _graphResult;
    private readonly Dictionary<SimplePos, uint> _posToId;
    private readonly SimplePos _startPos;

    public TranslocatorPathResult(Dictionary<SimplePos, SimplePos?> translocators, SimplePos startPos,
        SimplePos goalPos)
    {
        _startPos = startPos;
        _goalPos = goalPos;
        _graph = new Graph<SimplePos, string>();
        _posToId = new Dictionary<SimplePos, uint>();

        var startId = GetId(_startPos);
        var goalId = GetId(_goalPos);

        // 2. Add Edges: The "Big Walk" (Baseline)
        _graph.Connect(startId, goalId, (int)_startPos.DistanceTo(_goalPos), "Walk");

        // 3. Add Edges: Translocator Links
        foreach (var (src, target) in translocators)
            if (target.HasValue)
            {
                var srcId = GetId(src);
                var targetId = GetId(target.Value);

                // The ONLY way to get from src to target is the 0-cost jump
                _graph.Connect(srcId, targetId, 0, "Translocation");
                // Static translocators are 2-way, so add the return jump
                _graph.Connect(targetId, srcId, 0, "Translocation");

                // Distance from Start to the Entrance
                _graph.Connect(startId, srcId, (int)_startPos.DistanceTo(src), "Walk");
                _graph.Connect(startId, targetId, (int)_startPos.DistanceTo(target.Value), "Walk");

                // Distance from the Exit to the Goal
                _graph.Connect(srcId, goalId, (int)src.DistanceTo(_goalPos), "Walk");
                _graph.Connect(targetId, goalId, (int)target.Value.DistanceTo(_goalPos), "Walk");
            }

        // 4. Add Edges: Chaining (Portals near each other)
        var allTps = translocators.Where(kvp => kvp.Value.HasValue).ToList();
        foreach (var tp1 in allTps)
        {
            var exitPos = tp1.Value!.Value;

            // Find all potential next jumps, sort by distance, and take the closest 5-10
            var nearbyLinks = allTps
                .Where(tp2 => tp1.Key != tp2.Key)
                .Select(tp2 => new { Target = tp2.Key, Dist = exitPos.DistanceTo(tp2.Key) })
                .OrderBy(link => link.Dist)
                .Take(5); // Adjust this '5' based on performance needs

            foreach (var link in nearbyLinks)
            {
                var idA = GetId(exitPos);
                var idB = GetId(link.Target);

                // Connect both ways so the pathfinder can travel the "hallway" in either direction
                _graph.Connect(idA, idB, (int)link.Dist, "Walk");
                _graph.Connect(idB, idA, (int)link.Dist, "Walk");
            }
        }

        // 5. Calculate
        _graphResult = _graph.Dijkstra(startId, goalId);

        Path = _graphResult.GetPath().Select(id => _graph[id].Item).ToList().AsReadOnly();
    }

    public ReadOnlyCollection<SimplePos> Path { get; }

    private uint GetId(SimplePos p)
    {
        if (_posToId.TryGetValue(p, out var id)) return id;
        return _posToId[p] = _graph.AddNode(p);
    }

    public long GetBirdsEyeDistance()
    {
        return (long)_startPos.DistanceTo(_goalPos);
    }

    public long GetTotalDistance()
    {
        return _graphResult.Distance;
    }

    public bool IsFounded()
    {
        return _graphResult.IsFounded;
    }

    public SimplePos? GetNextStep()
    {
        if (Path.Count <= 1) return null;
        return Path[1];
    }
}