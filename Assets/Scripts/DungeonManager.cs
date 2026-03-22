using UnityEngine;
using System.Collections.Generic;

public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance;

    public RoomPreset[] presets;

    public TilemapRoomBuilder roomBuilder;
    public SpawnManager spawnManager;

    [Header("Room exit key (optional)")]
    [Tooltip("When enabled, room-exit triggers require a key before loading the next room.")]
    public bool requireKeyToChangeRoom;
    [Tooltip("Must match Key.keyID on pickups spawned for this dungeon.")]
    public string roomExitKeyId = "RedKey";
    public bool consumeKeyWhenChangingRoom = true;

    private bool[,] currentGrid;
    private int currentRoomIndex = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        LoadNextRoom("none"); // starting room
    }

    public void LoadNextRoom(string entryDirection)
{
    if (presets == null || presets.Length == 0)
    {
        Debug.LogError("No presets assigned!");
        return;
    }

    if (roomBuilder == null)
    {
        Debug.LogError("DungeonManager: roomBuilder is not assigned.");
        return;
    }

    if (spawnManager == null)
    {
        Debug.LogError("DungeonManager: spawnManager is not assigned.");
        return;
    }

    RoomPreset chosen = PickRandomPreset();
    if (chosen == null)
    {
        Debug.LogError("DungeonManager: presets array has no non-null RoomPreset entries. Remove empty slots or assign assets.");
        return;
    }

    currentRoomIndex++; // 🔥 USE IT HERE

    ClearOldDoors();

    currentGrid = chosen.Load();

    Debug.Log("Loading room #" + currentRoomIndex + " → " + chosen.name);

    roomBuilder.BuildFromGrid(currentGrid, chosen.width, chosen.height);

    PlaceDoors(chosen);

    SpawnPlayer(entryDirection);

    spawnManager.SpawnEntities(roomBuilder);
}

    RoomPreset PickRandomPreset()
    {
        int n = presets.Length;
        for (int attempt = 0; attempt < n * 4; attempt++)
        {
            RoomPreset p = presets[Random.Range(0, n)];
            if (p != null) return p;
        }
        return null;
    }

    void SpawnPlayer(string entryDirection)
{
    var player = GameObject.FindGameObjectWithTag("Player");
    if (player == null) return;

    int w = currentGrid.GetLength(0);
    int h = currentGrid.GetLength(1);

    Vector2Int spawnCell = new Vector2Int(w / 2, h / 2); // fallback

    // 🔥 find position near opposite door
    if (entryDirection == "left")
    {
        spawnCell = FindSpawnNearEdge(w - 3, true); // right side
    }
    else if (entryDirection == "right")
    {
        spawnCell = FindSpawnNearEdge(2, true); // left side
    }
    else if (entryDirection == "top")
    {
        spawnCell = FindSpawnNearEdge(h - 3, false); // bottom
    }
    else if (entryDirection == "bottom")
    {
        spawnCell = FindSpawnNearEdge(2, false); // top
    }
    else
    {
        // first room
        spawnCell = roomBuilder.FloorCells[Random.Range(0, roomBuilder.FloorCells.Count)];
    }

    Vector3 pos = roomBuilder.CellToWorld(spawnCell);

    player.transform.position = pos;

    var rb = player.GetComponent<Rigidbody2D>();
    if (rb != null) rb.linearVelocity = Vector2.zero;

    // camera follow
    var cam = Camera.main;
    if (cam != null)
    {
        var follow = cam.GetComponent<CameraFollow>();
        if (follow != null)
            follow.target = player.transform;
    }
}

Vector2Int FindSpawnNearEdge(int fixedCoord, bool vertical)
{
    List<Vector2Int> candidates = new List<Vector2Int>();

    int w = currentGrid.GetLength(0);
    int h = currentGrid.GetLength(1);

    if (vertical)
    {
        for (int y = 2; y < h - 2; y++)
        {
            if (currentGrid[fixedCoord, y])
                candidates.Add(new Vector2Int(fixedCoord, y));
        }
    }
    else
    {
        for (int x = 2; x < w - 2; x++)
        {
            if (currentGrid[x, fixedCoord])
                candidates.Add(new Vector2Int(x, fixedCoord));
        }
    }

    if (candidates.Count == 0)
        return new Vector2Int(w / 2, h / 2);

    return candidates[Random.Range(0, candidates.Count)];
}

void PlaceDoors(RoomPreset preset)
{
    var candidates = GetValidDoors(preset);

    if (candidates.Count < 2)
    {
        Debug.LogWarning("Not enough door candidates!");
        return;
    }

    // pick first door
    var doorA = candidates[Random.Range(0, candidates.Count)];

    // pick second door (different from first)
    DoorCandidate doorB;
    do
    {
        doorB = candidates[Random.Range(0, candidates.Count)];
    }
    while (doorB.pos == doorA.pos);

    CreateDoor(doorA);
    CreateDoor(doorB);
}

void CreateDoor(DoorCandidate door)
{
    int size = 5;
    int half = size / 2;

    if (door.direction == "left" || door.direction == "right")
    {
        // 🔥 vertical door
        for (int i = -half; i <= half; i++)
        {
            Vector2Int cell = new Vector2Int(door.pos.x, door.pos.y + i);
            roomBuilder.SetDoorCell(cell);
        }
    }
    else
    {
        // 🔥 horizontal door
        for (int i = -half; i <= half; i++)
        {
            Vector2Int cell = new Vector2Int(door.pos.x + i, door.pos.y);
            roomBuilder.SetDoorCell(cell);
        }
    }

    Vector3 pos = roomBuilder.CellToWorld(door.pos);

    GameObject trigger = new GameObject("DoorTrigger");
    trigger.tag = "Door";
    trigger.transform.position = pos;

    var col = trigger.AddComponent<BoxCollider2D>();
    col.isTrigger = true;

    // 🔥 match collider shape to door direction
    if (door.direction == "left" || door.direction == "right")
        col.size = new Vector2(1f, size);
    else
        col.size = new Vector2(size, 1f);

    var exit = trigger.AddComponent<RoomExit>();
    exit.direction = door.direction;
    exit.requireKey = requireKeyToChangeRoom;
    exit.requiredKeyId = roomExitKeyId;
    exit.consumeKeyOnExit = consumeKeyWhenChangingRoom;
}

void ClearOldDoors()
{
    GameObject[] oldDoors = GameObject.FindGameObjectsWithTag("Door");

    foreach (var d in oldDoors)
    {
        Destroy(d);
    }
}

class DoorCandidate
{
    public Vector2Int pos;
    public string direction; // "left", "right", "top", "bottom"
}

List<DoorCandidate> GetValidDoors(RoomPreset preset)
{
    List<DoorCandidate> valid = new List<DoorCandidate>();

    int w = preset.width;
    int h = preset.height;
    int size = 5;
    int half = size / 2;

    for (int x = 1; x < w - 1; x++)
    {
        for (int y = 1; y < h - 1; y++)
        {
            if (!currentGrid[x, y]) continue;

            // LEFT EDGE
            if (x == 1 && CanPlaceVerticalDoor(x, y, half, w, h))
                valid.Add(new DoorCandidate { pos = new Vector2Int(x, y), direction = "left" });

            // RIGHT EDGE
            if (x == w - 2 && CanPlaceVerticalDoor(x, y, half, w, h))
                valid.Add(new DoorCandidate { pos = new Vector2Int(x, y), direction = "right" });

            // BOTTOM EDGE
            if (y == 1 && CanPlaceHorizontalDoor(x, y, half, w, h))
                valid.Add(new DoorCandidate { pos = new Vector2Int(x, y), direction = "bottom" });

            // TOP EDGE
            if (y == h - 2 && CanPlaceHorizontalDoor(x, y, half, w, h))
                valid.Add(new DoorCandidate { pos = new Vector2Int(x, y), direction = "top" });
        }
    }

    return valid;
}

bool CanPlaceVerticalDoor(int x, int y, int half, int w, int h)
{
    for (int i = -half; i <= half; i++)
    {
        int ny = y + i;

        if (ny < 1 || ny >= h - 1)
            return false;

        if (!currentGrid[x, ny])
            return false;
    }

    return true;
}

bool CanPlaceHorizontalDoor(int x, int y, int half, int w, int h)
{
    for (int i = -half; i <= half; i++)
    {
        int nx = x + i;

        if (nx < 1 || nx >= w - 1)
            return false;

        if (!currentGrid[nx, y])
            return false;
    }

    return true;
}

    
}