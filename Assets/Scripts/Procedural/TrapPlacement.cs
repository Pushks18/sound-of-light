using UnityEngine;
using System.Collections.Generic;

public static class TrapPlacement
{
    [System.Serializable]
    public struct Rules
    {
        [Tooltip("Traps must be at least this far from the player spawn.")]
        public float minDistFromPlayer;
        [Tooltip("Minimum spacing between traps to prevent clustering.")]
        public float minDistBetweenTraps;
        [Tooltip("Traps must be at least this far from any enemy.")]
        public float minDistFromEnemies;
        [Tooltip("When true, corridors and chokepoints are preferred over open areas.")]
        public bool preferChokepoints;

        public static Rules Default => new Rules
        {
            minDistFromPlayer = 5f,
            minDistBetweenTraps = 4f,
            minDistFromEnemies = 3f,
            preferChokepoints = true
        };
    }

    /// <summary>
    /// Scores candidate floor cells and returns the best positions for trap placement.
    /// Enforces distance constraints and optionally favors chokepoints.
    /// </summary>
    public static List<Vector2Int> PickTrapCells(
        List<Vector2Int> candidateCells,
        TilemapRoomBuilder builder,
        int trapCount,
        Vector2 playerPos,
        List<Vector3> enemyPositions,
        Rules rules)
    {
        if (trapCount <= 0 || candidateCells.Count == 0)
            return new List<Vector2Int>();

        // Score every candidate cell
        var scored = new List<(Vector2Int cell, float score)>();

        foreach (var cell in candidateCells)
        {
            Vector3 worldPos = builder.CellToWorld(cell);

            // Hard rule: minimum distance from player
            if (Vector2.Distance(worldPos, playerPos) < rules.minDistFromPlayer)
                continue;

            // Hard rule: minimum distance from every enemy
            if (TooCloseToAny(worldPos, enemyPositions, rules.minDistFromEnemies))
                continue;

            float score = 0f;

            if (rules.preferChokepoints)
            {
                int neighbors = CountFloorNeighbors(builder, cell.x, cell.y);
                // 8 = wide open, 3-4 = corridor, 1-2 = dead-end nook
                // Corridors are the most strategic spots for traps.
                // Dead-ends are too hidden; open areas are too easy to dodge.
                score = neighbors switch
                {
                    <= 2 => 0.4f,  // dead-end nook — somewhat hidden
                    3    => 1.0f,  // tight corridor — best
                    4    => 0.9f,  // corridor bend / junction
                    5    => 0.7f,  // corridor opening into room
                    6    => 0.4f,  // near wall in open area
                    _    => 0.2f,  // wide open — easy to avoid
                };
            }
            else
            {
                score = 0.5f;
            }

            // Small random jitter so ties are broken unpredictably
            score += Random.value * 0.15f;

            scored.Add((cell, score));
        }

        // Sort descending — highest-scoring cells first
        scored.Sort((a, b) => b.score.CompareTo(a.score));

        // Greedily pick cells, enforcing minimum spacing between traps
        var picked = new List<Vector2Int>();
        var pickedPositions = new List<Vector3>();

        foreach (var (cell, score) in scored)
        {
            if (picked.Count >= trapCount)
                break;

            Vector3 worldPos = builder.CellToWorld(cell);

            if (TooCloseToAny(worldPos, pickedPositions, rules.minDistBetweenTraps))
                continue;

            picked.Add(cell);
            pickedPositions.Add(worldPos);
        }

        return picked;
    }

    static bool TooCloseToAny(Vector3 pos, List<Vector3> others, float minDist)
    {
        float minDistSq = minDist * minDist;
        foreach (var other in others)
        {
            if (((Vector2)(pos - other)).sqrMagnitude < minDistSq)
                return true;
        }
        return false;
    }

    static int CountFloorNeighbors(TilemapRoomBuilder builder, int x, int y)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            if (builder.IsFloor(x + dx, y + dy))
                count++;
        }
        return count;
    }
}
