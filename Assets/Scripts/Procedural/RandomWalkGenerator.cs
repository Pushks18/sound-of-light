using UnityEngine;
using System.Collections.Generic;

public class RandomWalkGenerator : MonoBehaviour
{
    [Header("Grid dimensions (in tiles)")]
    public int width = 100;
    public int height = 60;

    [Header("Walk parameters")]
    public int steps = 4000;
    public int walkers = 2;
    [Range(0f, 1f)] public float fillGoal = 0.45f;

    [Header("Tunnel Size")]
    public int carveRadius = 2;

    [Header("Wiring")]
    public TilemapRoomBuilder roomBuilder;
    public SpawnManager spawnManager;

    [Header("SAVE PRESET")]
    public RoomPreset presetToSave;

    [Header("Player Spawn")]
    public bool spawnPlayerOnGenerate = true;
    [Range(1, 8)] public int minFloorNeighboursForSpawn = 4;

    private bool[,] grid;

    void Start()
    {
        Generate();
    }

    public void Generate()
    {
        if (roomBuilder == null)
        {
            Debug.LogError("roomBuilder NOT assigned!");
            return;
        }

        grid = BuildGrid();

        if (presetToSave != null)
        {
            presetToSave.Save(grid);
            Debug.Log("Saved preset to: " + presetToSave.name);
        }

        roomBuilder.BuildFromGrid(grid, width, height);

        if (spawnPlayerOnGenerate)
            SpawnPlayer();

        spawnManager?.SpawnEntities(roomBuilder);
    }

    bool[,] BuildGrid()
    {
        bool[,] g = new bool[width, height];

        for (int w = 0; w < walkers; w++)
        {
            int cx = width / 2;
            int cy = height / 2;

            for (int i = 0; i < steps; i++)
            {
                CarveCircle(g, cx, cy);

                int dir = Random.Range(0, 4);
                if (dir == 0 && cx < width - 2) cx++;
                else if (dir == 1 && cx > 1) cx--;
                else if (dir == 2 && cy < height - 2) cy++;
                else if (dir == 3 && cy > 1) cy--;

                if (i % 200 == 0 && FloorRatio(g) >= fillGoal)
                    break;
            }
        }

        return g;
    }

    void CarveCircle(bool[,] g, int x, int y)
    {
        for (int dx = -carveRadius; dx <= carveRadius; dx++)
        {
            for (int dy = -carveRadius; dy <= carveRadius; dy++)
            {
                if (dx * dx + dy * dy > carveRadius * carveRadius)
                    continue;

                int nx = x + dx;
                int ny = y + dy;

                if (nx >= 1 && nx < width - 1 && ny >= 1 && ny < height - 1)
                    g[nx, ny] = true;
            }
        }
    }

    float FloorRatio(bool[,] g)
    {
        int count = 0;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (g[x, y]) count++;

        return (float)count / (width * height);
    }

    void SpawnPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player not found!");
            return;
        }

        List<Vector2Int> safe = new List<Vector2Int>();

        for (int x = 1; x < width - 1; x++)
        for (int y = 1; y < height - 1; y++)
        {
            if (!grid[x, y]) continue;

            int neighbors = 0;
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (grid[x + dx, y + dy]) neighbors++;

            if (neighbors >= minFloorNeighboursForSpawn)
                safe.Add(new Vector2Int(x, y));
        }

        if (safe.Count == 0)
        {
            Debug.LogWarning("No safe spawn cells found!");
            return;
        }

        var chosen = safe[Random.Range(0, safe.Count)];
        Vector3 worldPos = roomBuilder.CellToWorld(chosen);

        player.transform.position = worldPos;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        Debug.Log("Player spawn: " + worldPos);

        var cam = Camera.main;
        if (cam != null)
        {
            var follow = cam.GetComponent<CameraFollow>();
            if (follow != null)
                follow.target = player.transform;
        }
    }
}