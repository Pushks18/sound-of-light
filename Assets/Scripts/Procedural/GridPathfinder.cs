using UnityEngine;
using System.Collections.Generic;

public static class GridPathfinder
{
    static readonly Vector2Int[] Dirs =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),   // cardinal
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)   // diagonal
    };

    /// <summary>
    /// A* pathfinding on a tile grid with 8-directional movement.
    /// Returns a list of grid cells from start to goal, or null if no path exists.
    /// </summary>
    public static List<Vector2Int> FindPath(
        Vector2Int start,
        Vector2Int goal,
        System.Func<int, int, bool> isWalkable,
        int maxIterations = 3000)
    {
        if (start == goal)
            return new List<Vector2Int> { start };

        if (!isWalkable(start.x, start.y) || !isWalkable(goal.x, goal.y))
            return null;

        var open = new List<(Vector2Int pos, float f)>(64);
        open.Add((start, Heuristic(start, goal)));

        var closed = new HashSet<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float> { [start] = 0f };

        int iter = 0;

        while (open.Count > 0 && iter++ < maxIterations)
        {
            // Pop node with lowest f-score
            int best = 0;
            for (int i = 1; i < open.Count; i++)
            {
                if (open[i].f < open[best].f)
                    best = i;
            }

            Vector2Int current = open[best].pos;
            open.RemoveAt(best);

            if (current == goal)
                return Reconstruct(cameFrom, current);

            if (!closed.Add(current))
                continue; // already fully expanded

            float currentG = gScore[current];

            for (int d = 0; d < 8; d++)
            {
                Vector2Int neighbor = current + Dirs[d];

                if (closed.Contains(neighbor))
                    continue;
                if (!isWalkable(neighbor.x, neighbor.y))
                    continue;

                // Prevent corner-cutting through diagonal walls
                if (d >= 4)
                {
                    if (!isWalkable(current.x + Dirs[d].x, current.y) ||
                        !isWalkable(current.x, current.y + Dirs[d].y))
                        continue;
                }

                float cost = d >= 4 ? 1.414f : 1f;
                float tentG = currentG + cost;

                if (tentG < gScore.GetValueOrDefault(neighbor, float.MaxValue))
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentG;
                    open.Add((neighbor, tentG + Heuristic(neighbor, goal)));
                }
            }
        }

        return null; // no path found
    }

    // Octile distance heuristic for 8-directional grids
    static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy) + 0.414f * Mathf.Min(dx, dy);
    }

    static List<Vector2Int> Reconstruct(
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        Vector2Int current)
    {
        var path = new List<Vector2Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }
}
