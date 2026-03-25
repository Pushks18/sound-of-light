using UnityEngine;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    public GameObject enemyPrefab;
    public GameObject trapPrefab;
    public GameObject keyPrefab;

    public int enemyCount = 10;
    public int trapCount = 5;
    public int keyCount;

    public float minDistanceFromPlayer = 6f;
    public float minDistanceBetweenEnemies = 4f;
    public float edgeBuffer = 4f;

    [Header("Trap Placement Rules")]
    public TrapPlacement.Rules trapRules = TrapPlacement.Rules.Default;

    /// <summary>
    /// Spawns enemies, traps, and keys. Returns the number of enemies spawned.
    /// </summary>
    public int SpawnEntities(TilemapRoomBuilder builder)
    {
        if (builder.FloorCells.Count == 0) return 0;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return 0;

        Vector2 playerPos = player.transform.position;

        List<Vector2Int> validCells = new List<Vector2Int>();

        int w = builder.width;
        int h = builder.height;

        foreach (var cell in builder.FloorCells)
        {
            if (cell.x < edgeBuffer || cell.x > w - edgeBuffer) continue;
            if (cell.y < edgeBuffer || cell.y > h - edgeBuffer) continue;

            validCells.Add(cell);
        }

        // Shuffle for enemy/key placement
        for (int i = 0; i < validCells.Count; i++)
        {
            int j = Random.Range(i, validCells.Count);
            var temp = validCells[i];
            validCells[i] = validCells[j];
            validCells[j] = temp;
        }

        // --- Spawn enemies ---
        int spawned = 0;
        List<Vector3> enemyPositions = new List<Vector3>();

        if (enemyPrefab != null)
        {
            float minEnemyDistSq = minDistanceBetweenEnemies * minDistanceBetweenEnemies;

            foreach (var cell in validCells)
            {
                if (spawned >= enemyCount) break;

                Vector3 pos = builder.CellToWorld(cell);

                if (Vector2.Distance(pos, playerPos) < minDistanceFromPlayer)
                    continue;

                // Enforce spacing between enemies
                bool tooClose = false;
                foreach (var ep in enemyPositions)
                {
                    if (((Vector2)(pos - ep)).sqrMagnitude < minEnemyDistSq)
                    { tooClose = true; break; }
                }
                if (tooClose) continue;

                Instantiate(enemyPrefab, pos, Quaternion.identity);
                enemyPositions.Add(pos);
                spawned++;
            }
        }

        int enemiesSpawned = spawned;

        // --- Spawn traps using placement rules ---
        if (trapPrefab != null && trapCount > 0)
        {
            var trapCells = TrapPlacement.PickTrapCells(
                validCells, builder, trapCount,
                playerPos, enemyPositions, trapRules);

            foreach (var cell in trapCells)
            {
                Vector3 pos = builder.CellToWorld(cell);
                Instantiate(trapPrefab, pos, Quaternion.identity);
            }
        }

        // --- Spawn keys ---
        if (keyPrefab != null && keyCount > 0)
        {
            spawned = 0;
            foreach (var cell in validCells)
            {
                if (spawned >= keyCount) break;

                Vector3 pos = builder.CellToWorld(cell);

                if (Vector2.Distance(pos, playerPos) < minDistanceFromPlayer)
                    continue;

                Instantiate(keyPrefab, pos, Quaternion.identity);
                spawned++;
            }
        }

        return enemiesSpawned;
    }
}