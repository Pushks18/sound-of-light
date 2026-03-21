using UnityEngine;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    public GameObject enemyPrefab;
    public GameObject trapPrefab;

    public int enemyCount = 10;
    public int trapCount = 5;

    public float minDistanceFromPlayer = 6f;
    public float edgeBuffer = 4f;

    public void SpawnEntities(TilemapRoomBuilder builder)
    {
        if (builder.FloorCells.Count == 0) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Vector2 playerPos = player.transform.position;

        List<Vector2Int> validCells = new List<Vector2Int>();

        int w = builder.width;
        int h = builder.height;

        foreach (var cell in builder.FloorCells)
        {
            // 🔥 remove edge cells
            if (cell.x < edgeBuffer || cell.x > w - edgeBuffer) continue;
            if (cell.y < edgeBuffer || cell.y > h - edgeBuffer) continue;

            validCells.Add(cell);
        }

        // 🔥 shuffle
        for (int i = 0; i < validCells.Count; i++)
        {
            int j = Random.Range(i, validCells.Count);
            var temp = validCells[i];
            validCells[i] = validCells[j];
            validCells[j] = temp;
        }

        int spawned = 0;

        foreach (var cell in validCells)
        {
            if (spawned >= enemyCount) break;

            Vector3 pos = builder.CellToWorld(cell);

            if (Vector2.Distance(pos, playerPos) < minDistanceFromPlayer)
                continue;

            Instantiate(enemyPrefab, pos, Quaternion.identity);
            spawned++;
        }

        spawned = 0;

        foreach (var cell in validCells)
        {
            if (spawned >= trapCount) break;

            Vector3 pos = builder.CellToWorld(cell);

            Instantiate(trapPrefab, pos, Quaternion.identity);
            spawned++;
        }
    }
}