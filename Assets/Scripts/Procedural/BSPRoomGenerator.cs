using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public class BSPRoomGenerator : MonoBehaviour
{
    [Header("Grid")]
    public int width  = 128;
    public int height = 64;

    [Header("BSP")]
    public int minLeafSize   = 8;
    public int maxLeafSize   = 20;
    public int maxLeaves     = 20;
    public int corridorWidth = 1;

    [Header("Visual")]
    public Color floorColor = Color.white;
    public Color wallColor  = Color.black;
    public int   pixelSize  = 4;   // grid cell → texture pixels

    private bool[,] grid;

    // ------------------------------------------------------------------ lifecycle

    void Start()
    {
        Generate();
    }

    // ------------------------------------------------------------------ public API

    /// <summary>Re-generate the dungeon layout. Safe to call from Inspector buttons or other scripts.</summary>
    public void Generate()
    {
        InitGrid();

        // ---- BSP split phase --------------------------------------------------
        Leaf root   = new Leaf(0, 0, width, height);
        var  leaves = new List<Leaf> { root };

        bool didSplit = true;
        while (didSplit && leaves.Count < maxLeaves)
        {
            didSplit = false;
            // iterate over snapshot — we'll add children while iterating
            int count = leaves.Count;
            for (int i = 0; i < count; i++)
            {
                Leaf leaf = leaves[i];
                if (leaf.left != null || leaf.right != null) continue; // already split

                bool tooBig = leaf.width > maxLeafSize || leaf.height > maxLeafSize;
                if (tooBig || Random.value > 0.7f)
                {
                    if (leaf.Split(minLeafSize))
                    {
                        leaves.Add(leaf.left);
                        leaves.Add(leaf.right);
                        didSplit = true;
                    }
                }
            }
        }

        // ---- Room creation & carving ------------------------------------------
        root.CreateRooms();
        List<RoomRect> rooms = root.GetRooms();

        foreach (var r in rooms)
            CarveRect(r.x, r.y, r.w, r.h);

        // ---- Connect rooms with L-shaped corridors ----------------------------
        for (int i = 1; i < rooms.Count; i++)
        {
            Vector2Int a = rooms[i - 1].Center();
            Vector2Int b = rooms[i].Center();
            CarveCorridor(a, b);
        }

        ApplyTextureToSprite(ExportTexture());
    }

    // ------------------------------------------------------------------ grid helpers

    void InitGrid()
    {
        grid = new bool[width, height];
        for (int x = 0; x < width;  x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = false;
    }

    void CarveRect(int ox, int oy, int w, int h)
    {
        for (int x = ox; x < ox + w; x++)
        for (int y = oy; y < oy + h; y++)
        {
            if (x > 0 && x < width - 1 && y > 0 && y < height - 1)
                grid[x, y] = true;
        }
    }

    /// <summary>Carves an L-shaped corridor (horizontal first, then vertical).</summary>
    void CarveCorridor(Vector2Int a, Vector2Int b)
    {
        int half = corridorWidth / 2;
        int x = a.x;
        int y = a.y;

        // horizontal segment
        while (x != b.x)
        {
            for (int dy = -half; dy <= half; dy++)
                if (y + dy > 0 && y + dy < height - 1) grid[x, y + dy] = true;
            x += (b.x > x) ? 1 : -1;
        }

        // vertical segment
        while (y != b.y)
        {
            for (int dx = -half; dx <= half; dx++)
                if (x + dx > 0 && x + dx < width - 1) grid[x + dx, y] = true;
            y += (b.y > y) ? 1 : -1;
        }
    }

    // ------------------------------------------------------------------ texture / sprite

    Texture2D ExportTexture()
    {
        int texW = width  * pixelSize;
        int texH = height * pixelSize;

        Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int x = 0; x < width;  x++)
        for (int y = 0; y < height; y++)
        {
            Color c = grid[x, y] ? floorColor : wallColor;
            for (int px = 0; px < pixelSize; px++)
            for (int py = 0; py < pixelSize; py++)
                tex.SetPixel(x * pixelSize + px, y * pixelSize + py, c);
        }

        tex.Apply();
        return tex;
    }

    void ApplyTextureToSprite(Texture2D tex)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Sprite sp = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            pixelSize);
        sr.sprite = sp;
    }

    // ------------------------------------------------------------------ public data access

    /// <summary>Returns the raw grid so other systems (spawners, colliders) can query it.</summary>
    public bool[,] GetGrid() => grid;

    // ================================================================== BSP helpers

    /// <summary>Simple axis-aligned rectangle used for rooms.</summary>
    class RoomRect
    {
        public int x, y, w, h;
        public RoomRect(int x, int y, int w, int h) { this.x = x; this.y = y; this.w = w; this.h = h; }
        public Vector2Int Center() => new Vector2Int(x + w / 2, y + h / 2);
    }

    /// <summary>BSP tree node — stores a region and optionally a child split and an interior room.</summary>
    class Leaf
    {
        public int x, y, width, height;
        public Leaf    left,  right;
        public RoomRect room;

        public Leaf(int x, int y, int w, int h)
        {
            this.x = x; this.y = y; width = w; height = h;
        }

        /// <summary>Split this leaf into two children. Returns false if it's too small to split.</summary>
        public bool Split(int minSize)
        {
            // choose orientation: favour the longer axis; random when roughly square
            bool splitH;
            if        ((float)width / height >= 1.25f) splitH = false;
            else if   ((float)height / width >= 1.25f) splitH = true;
            else      splitH = Random.value > 0.5f;

            if (splitH)
            {
                int max = height - minSize;
                if (max <= minSize) return false;
                int split = Random.Range(minSize, max);
                left  = new Leaf(x, y,          width, split);
                right = new Leaf(x, y + split,  width, height - split);
            }
            else
            {
                int max = width - minSize;
                if (max <= minSize) return false;
                int split = Random.Range(minSize, max);
                left  = new Leaf(x,         y, split,        height);
                right = new Leaf(x + split, y, width - split, height);
            }
            return true;
        }

        /// <summary>Recursively create rooms; leaves create an interior rect, branches recurse.</summary>
        public void CreateRooms()
        {
            if (left != null || right != null)
            {
                left?.CreateRooms();
                right?.CreateRooms();
                return;
            }

            // create a room with random size fitted inside this leaf (with ≥1 cell margin)
            int roomW = Random.Range(Mathf.Max(3, width  / 2), Mathf.Max(4, width  - 2));
            int roomH = Random.Range(Mathf.Max(3, height / 2), Mathf.Max(4, height - 2));
            int roomX = Random.Range(x + 1, x + width  - roomW - 1);
            int roomY = Random.Range(y + 1, y + height - roomH - 1);

            room = new RoomRect(roomX, roomY, roomW, roomH);
        }

        /// <summary>Collect all rooms in this subtree.</summary>
        public List<RoomRect> GetRooms()
        {
            var list = new List<RoomRect>();
            if (room != null) list.Add(room);
            if (left  != null) list.AddRange(left.GetRooms());
            if (right != null) list.AddRange(right.GetRooms());
            return list;
        }
    }
}
