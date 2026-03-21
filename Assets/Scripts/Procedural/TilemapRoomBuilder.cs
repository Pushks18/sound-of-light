using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Converts a bool[,] grid into Floor + Wall Tilemap tiles.
///
/// Attach to: the Grid GameObject.
/// Wire in Inspector: floorTilemap, wallTilemap, floorTile, wallTile.
///
/// Wall collision:
///   WallTilemap needs:
///     • TilemapCollider2D  (Composite Operation = Merge)
///     • CompositeCollider2D
///     • Rigidbody2D        (Body Type = Static)
/// </summary>
public class TilemapRoomBuilder : MonoBehaviour
{
    [Header("Tilemap references")]
    public Tilemap floorTilemap;
    public Tilemap wallTilemap;
    public Tilemap doorTilemap;

    public int width;
public int height;


    [Header("Tile assets (assign in Inspector)")]
    public TileBase floorTile;
    public TileBase wallTile;
    public TileBase doorTile;

    [Header("Colors")]
    [Tooltip("Base color for floor tiles. Alpha is preserved from the tile sprite.")]
    public Color floorBaseColor  = new Color(0.72f, 0.68f, 0.60f); // warm stone
    [Tooltip("Base color for wall tiles — usually much darker than the floor.")]
    public Color wallBaseColor   = new Color(0.18f, 0.16f, 0.14f); // near-black
    [Tooltip("Perlin noise scale — larger values = coarser variation across the cave.")]
    public float noiseScale      = 0.12f;
    [Tooltip("How much the noise darkens floor tiles. 0 = flat color, 1 = full range.")]
    [Range(0f, 1f)] public float noiseStrength = 0.25f;

    // All floor-cell positions in tilemap space (integer coords).
    // SpawnManager reads this to find valid spawn positions.
    public IReadOnlyList<Vector2Int> FloorCells => floorCells;
    private List<Vector2Int> floorCells = new List<Vector2Int>();

    // Centre of the carved region in world space — used to place the player at start.
    public Vector3 FloorCentre { get; private set; }

    // ------------------------------------------------------------------ public API

    /// <summary>
    /// Clear both tilemaps and paint tiles according to grid.
    /// grid[x,y] == true  → floor tile
    /// grid[x,y] == false → wall  tile
    /// </summary>
    public void BuildFromGrid(bool[,] grid, int w, int h)
{
    width = w;
    height = h;
        if (floorTile == null || wallTile == null)
        {
            Debug.LogError("[TilemapRoomBuilder] floorTile or wallTile is not assigned!", this);
            return;
        }

        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
        doorTilemap.ClearAllTiles();
        floorCells.Clear();

        Vector2 centreSum = Vector2.zero;
        int     floorCount = 0;

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            var cell = new Vector3Int(x, y, 0);

            if (grid[x, y])
            {
                floorTilemap.SetTile(cell, floorTile);
                floorCells.Add(new Vector2Int(x, y));

                // Apply Perlin-noise color variation for a natural stone feel
                float noise  = Mathf.PerlinNoise(x * noiseScale, y * noiseScale);
                float dark   = 1f - noiseStrength * noise;          // range: [1-noiseStrength .. 1]
                Color fColor = new Color(
                    floorBaseColor.r * dark,
                    floorBaseColor.g * dark,
                    floorBaseColor.b * dark,
                    floorBaseColor.a);
                floorTilemap.SetColor(cell, fColor);

                centreSum += new Vector2(x + 0.5f, y + 0.5f);
                floorCount++;
            }
            else
            {
                wallTilemap.SetTile(cell, wallTile);
                wallTilemap.SetColor(cell, wallBaseColor);
            }
        }

        FloorCentre = floorCount > 0
            ? new Vector3(centreSum.x / floorCount, centreSum.y / floorCount, 0f)
            : Vector3.zero;

        Debug.Log($"[TilemapRoomBuilder] Built {floorCount} floor cells. Centre ≈ {FloorCentre}");
    }

    public void SetDoorCell(Vector2Int cell)
{
    Vector3Int tilePos = new Vector3Int(cell.x, cell.y, 0);

    // remove wall
    wallTilemap.SetTile(tilePos, null);

    // place door tile
    doorTilemap.SetTile(tilePos, doorTile);
}

    // ------------------------------------------------------------------ utility

    /// <summary>
    /// Returns true when the cell at (x,y) is walkable floor.
    /// Bounds-safe.
    /// </summary>
    public bool IsFloor(int x, int y)
    {
        var cell = new Vector3Int(x, y, 0);
        return floorTilemap.HasTile(cell);
    }

    /// <summary>
    /// Converts a tilemap cell coordinate to world position (cell centre).
    /// </summary>
    public Vector3 CellToWorld(Vector2Int cell)
        => floorTilemap.CellToWorld(new Vector3Int(cell.x, cell.y, 0))
           + floorTilemap.cellSize * 0.5f;
}
