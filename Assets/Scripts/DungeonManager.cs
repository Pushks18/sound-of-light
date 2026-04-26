using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using System.Collections;
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

    // ── Progressive Mode ────────────────────────────────────────────────
    [Header("Progressive Mode (live generation)")]
    [Tooltip("When enabled, rooms are generated live with scaling difficulty instead of loading presets.")]
    public bool progressiveMode;

    [Header("Starting Parameters (Room 1)")]
    public int startWidth = 30;
    public int startHeight = 22;
    public int startEnemies = 3;
    public int startTraps = 1;

    [Header("Scaling Per Room")]
    public int widthGrowth = 4;
    public int heightGrowth = 3;
    public int maxWidth = 65;
    public int maxHeight = 45;
    public int enemyGrowthPerRoom = 1;
    public float trapGrowthPerRoom = 0.5f;
    public int maxEnemies = 20;
    public int maxTraps = 12;

    [Header("Between-Floor Healing")]
    [Tooltip("HP healed after clearing a room. Decreases by healDecay each room.")]
    public int healPerFloor = 3;
    public int healDecay = 1;
    public int minHeal = 1;

    [Header("Spawn Settings")]
    [Tooltip("Flash radius on room entry. Enemies won't spawn inside this radius.")]
    public float spawnFlashRadius = 10f;
    [Tooltip("Minimum distance between spawned enemies to prevent clustering.")]
    public float minDistBetweenEnemies = 4f;
    public float spawnFlashIntensity = 2f;
    public float spawnFlashDuration = 3f;

    [Header("Generation Tuning")]
    public int genWalkers = 2;
    [Range(0.25f, 0.55f)] public float genFillGoal = 0.32f;
    public int genCarveRadius = 1;

    [Header("Trap Placement Rules (progressive mode)")]
    public TrapPlacement.Rules trapRules = TrapPlacement.Rules.Default;

    [Header("Spawn Prefabs (progressive mode)")]
    public GameObject enemyPrefab;
    public GameObject skitterPrefab;
    [Tooltip("Fraction of the enemy budget spawned as Skitters (0 = none, 1 = all). 0.6 = generously Skitter-heavy.")]
    [Range(0f, 1f)]
    public float skitterFraction = 0.6f;
    public GameObject trapPrefab;
    public GameObject keyPrefab;

    [Header("Boss Room (progressive mode)")]
    [Tooltip("Every N rooms a boss scene is loaded. Set 0 to disable.")]
    public int bossRoomInterval = 5;
    [Tooltip("Boss scenes loaded in order, cycling. Must be in Build Settings.")]
    public string[] bossSceneNames = { "VesperScene", "ScarabScene", "GoblinBossScene", "IsshinBossScene" };
    // ─────────────────────────────────────────────────────────────────────

    // ── Cross-scene run state (static — survives scene loads) ───────────────
    /// <summary>Set before entering a boss scene so GameManager.BossDefeated knows where to return.</summary>
    public static bool   IsReturningFromBoss = false;
    public static int    RoomIndexBeforeBoss = 0;
    public static string OriginSceneName     = "";
    // Boss pool: shuffled draw-without-replacement; refills automatically when empty.
    static readonly System.Collections.Generic.List<string> bossPool = new();
    // ────────────────────────────────────────────────────────────────────────

    private bool[,] currentGrid;
    private int  currentRoomIndex   = 0;
    private bool _nextRoomIsPostBoss = false;

    public int CurrentRoomIndex => currentRoomIndex;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        EnsureEndlessComponents();

        // Returning from a boss scene — restore room index so the run continues correctly
        if (IsReturningFromBoss)
        {
            currentRoomIndex    = RoomIndexBeforeBoss;  // next call increments to +1
            _nextRoomIsPostBoss = true;
            IsReturningFromBoss = false;
            RoomIndexBeforeBoss = 0;
            OriginSceneName     = "";
        }

        LoadNextRoom("none");
    }

    void EnsureEndlessComponents()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        if (GameManager.Instance == null)
        {
            var gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        }

        if (FindAnyObjectByType<MinimapRadar>() == null)
            player.AddComponent<MinimapRadar>();

        if (FindAnyObjectByType<RoomCounterHUD>() == null)
            player.AddComponent<RoomCounterHUD>();
    }

    public Coroutine LoadNextRoom(string entryDirection)
    {
        return StartCoroutine(LoadNextRoomRoutine(entryDirection));
    }

    IEnumerator LoadNextRoomRoutine(string entryDirection)
    {
        if (roomBuilder == null)
        {
            Debug.LogError("DungeonManager: roomBuilder is not assigned.");
            yield break;
        }

        currentRoomIndex++;

        // ── Boss room: heal player then hand off to the dedicated boss scene ──
        bool isBossRoom = progressiveMode
                          && bossSceneNames != null && bossSceneNames.Length > 0
                          && bossRoomInterval > 0
                          && (currentRoomIndex % bossRoomInterval == 0);

        if (isBossRoom)
        {
            // Refill pool when empty
            if (bossPool.Count == 0)
            {
                bossPool.AddRange(bossSceneNames);
                // Fisher-Yates shuffle
                for (int i = bossPool.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (bossPool[i], bossPool[j]) = (bossPool[j], bossPool[i]);
                }
            }
            string nextBoss = bossPool[bossPool.Count - 1];
            bossPool.RemoveAt(bossPool.Count - 1);

            // Apply normal per-floor heal before leaving (user decision: boss entry = normal heal)
            if (currentRoomIndex > 1)
                HealPlayer();

            // Save run state so DungeonManager.Start can restore it on return
            IsReturningFromBoss = true;
            RoomIndexBeforeBoss = currentRoomIndex;
            OriginSceneName     = SceneManager.GetActiveScene().name;

            SceneManager.LoadScene(nextBoss);
            yield break;   // stop coroutine — scene is loading
        }
        // ─────────────────────────────────────────────────────────────────────

        ClearOldDoors();
        ClearOldEntities();

        // Build tilemap first (generates colliders)
        if (progressiveMode)
            BuildProgressiveRoom();
        else
            BuildPresetRoom();

        // Wait a physics frame so TilemapCollider2D finishes rebuilding wall colliders
        yield return new WaitForFixedUpdate();

        // Now safe to place entities — colliders are solid
        SpawnPlayer(entryDirection);

        int newEnemyCount;
        if (progressiveMode)
        {
            int room    = currentRoomIndex;
            int enemies = Mathf.Min(startEnemies + enemyGrowthPerRoom * (room - 1), maxEnemies);
            int traps   = Mathf.Min(startTraps + Mathf.FloorToInt(trapGrowthPerRoom * (room - 1)), maxTraps);
            newEnemyCount = SpawnProgressiveEntities(enemies, traps);
        }
        else
        {
            newEnemyCount = spawnManager != null ? spawnManager.SpawnEntities(roomBuilder) : 0;
        }

        GameManager.Instance?.ResetForNewRoom(newEnemyCount);
        RoomCounterHUD.Instance?.UpdateRoom(currentRoomIndex);

        if (progressiveMode)
        {
            if (currentRoomIndex > 1)
            {
                if (_nextRoomIsPostBoss)
                {
                    HealPostBoss();
                    _nextRoomIsPostBoss = false;
                }
                else
                {
                    HealPlayer();
                }
            }
            EmitSpawnFlash();
        }
    }

    // ── Preset-based room loading (original) ────────────────────────────

    void BuildPresetRoom()
    {
        if (presets == null || presets.Length == 0)
        {
            Debug.LogError("No presets assigned!");
            return;
        }

        RoomPreset chosen = PickRandomPreset();
        if (chosen == null)
        {
            Debug.LogError("DungeonManager: no valid presets.");
            return;
        }

        currentGrid = chosen.Load();
        Debug.Log("Loading room #" + currentRoomIndex + " -> " + chosen.name);

        roomBuilder.BuildFromGrid(currentGrid, chosen.width, chosen.height);
        PlaceDoors(chosen.width, chosen.height);
    }

    // ── Progressive live generation ─────────────────────────────────────

    void BuildProgressiveRoom()
    {
        int room = currentRoomIndex;
        int w = Mathf.Min(startWidth + widthGrowth * (room - 1), maxWidth);
        int h = Mathf.Min(startHeight + heightGrowth * (room - 1), maxHeight);

        Debug.Log($"Progressive room #{room}: {w}x{h}");

        currentGrid = GenerateGrid(w, h);
        roomBuilder.BuildFromGrid(currentGrid, w, h);
    }

    bool[,] GenerateGrid(int w, int h)
    {
        bool[,] g = new bool[w, h];

        for (int walker = 0; walker < genWalkers; walker++)
        {
            int cx = w / 2;
            int cy = h / 2;

            int maxSteps = w * h * 4;
            for (int i = 0; i < maxSteps; i++)
            {
                CarveCircle(g, cx, cy, w, h);

                int dir = Random.Range(0, 4);
                if (dir == 0 && cx < w - 2) cx++;
                else if (dir == 1 && cx > 1) cx--;
                else if (dir == 2 && cy < h - 2) cy++;
                else if (dir == 3 && cy > 1) cy--;

                if (i % 200 == 0 && FloorRatio(g, w, h) >= genFillGoal)
                    break;
            }
        }

        WidenNarrowPassages(g, w, h);
        return g;
    }

    // Detects 1-wide corridors and widens them so the player can always fit through.
    void WidenNarrowPassages(bool[,] g, int w, int h)
    {
        bool[,] snapshot = (bool[,])g.Clone();

        for (int x = 1; x < w - 1; x++)
        for (int y = 1; y < h - 1; y++)
        {
            if (!snapshot[x, y]) continue;

            // Horizontal pinch: walls above and below → widen vertically
            if (!snapshot[x, y - 1] && !snapshot[x, y + 1])
            {
                if (y + 1 < h - 1) g[x, y + 1] = true;
                else if (y - 1 >= 1) g[x, y - 1] = true;
            }

            // Vertical pinch: walls left and right → widen horizontally
            if (!snapshot[x - 1, y] && !snapshot[x + 1, y])
            {
                if (x + 1 < w - 1) g[x + 1, y] = true;
                else if (x - 1 >= 1) g[x - 1, y] = true;
            }
        }
    }

    void CarveCircle(bool[,] g, int x, int y, int w, int h)
    {
        for (int dx = -genCarveRadius; dx <= genCarveRadius; dx++)
        for (int dy = -genCarveRadius; dy <= genCarveRadius; dy++)
        {
            if (dx * dx + dy * dy > genCarveRadius * genCarveRadius)
                continue;
            int nx = x + dx, ny = y + dy;
            if (nx >= 1 && nx < w - 1 && ny >= 1 && ny < h - 1)
                g[nx, ny] = true;
        }
    }

    float FloorRatio(bool[,] g, int w, int h)
    {
        int count = 0;
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (g[x, y]) count++;
        return (float)count / (w * h);
    }

    int SpawnProgressiveEntities(int enemyTarget, int trapTarget)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return 0;

        Vector2 playerPos = player.transform.position;
        int w = currentGrid.GetLength(0);
        int h = currentGrid.GetLength(1);

        // Gather valid cells (away from edges)
        List<Vector2Int> validCells = new List<Vector2Int>();
        float edgeBuf = 3f;
        foreach (var cell in roomBuilder.FloorCells)
        {
            if (cell.x < edgeBuf || cell.x > w - edgeBuf) continue;
            if (cell.y < edgeBuf || cell.y > h - edgeBuf) continue;
            validCells.Add(cell);
        }

        // Shuffle for enemy placement
        for (int i = 0; i < validCells.Count; i++)
        {
            int j = Random.Range(i, validCells.Count);
            var temp = validCells[i];
            validCells[i] = validCells[j];
            validCells[j] = temp;
        }

        // --- Spawn enemies (outside spawn flash radius) ---
        // Split budget: skitterFraction of slots go to Skitter, remainder to base enemy.
        int skitterTarget = (skitterPrefab != null)
            ? Mathf.RoundToInt(enemyTarget * skitterFraction)
            : 0;
        int baseTarget = enemyTarget - skitterTarget;

        int enemiesSpawned  = 0;
        int skittersSpawned = 0;
        List<Vector3> enemyPositions = new List<Vector3>();

        float minEnemyDistSq = minDistBetweenEnemies * minDistBetweenEnemies;
        // Same exclusion zone as base enemies — flee-with-wall-repulsion handles the rest.
        float skitterMinDist = spawnFlashRadius;

        foreach (var cell in validCells)
        {
            if (enemiesSpawned >= baseTarget && skittersSpawned >= skitterTarget) break;

            Vector3 pos = roomBuilder.CellToWorld(cell);
            float distToPlayer = Vector2.Distance(pos, playerPos);
            if (distToPlayer < spawnFlashRadius) continue;

            bool tooClose = false;
            foreach (var ep in enemyPositions)
            {
                if (((Vector2)(pos - ep)).sqrMagnitude < minEnemyDistSq)
                { tooClose = true; break; }
            }
            if (tooClose) continue;

            // Alternate between types so they spread naturally across the room.
            bool wantSkitter = skittersSpawned < skitterTarget
                && (enemiesSpawned >= baseTarget || skittersSpawned <= enemiesSpawned);
            bool wantBase    = enemiesSpawned < baseTarget && enemyPrefab != null;

            if (wantSkitter && distToPlayer >= skitterMinDist)
            {
                Instantiate(skitterPrefab, pos, Quaternion.identity);
                skittersSpawned++;
                enemyPositions.Add(pos);
            }
            else if (wantBase)
            {
                Instantiate(enemyPrefab, pos, Quaternion.identity);
                enemiesSpawned++;
                enemyPositions.Add(pos);
            }
            else if (skittersSpawned < skitterTarget && distToPlayer >= skitterMinDist)
            {
                Instantiate(skitterPrefab, pos, Quaternion.identity);
                skittersSpawned++;
                enemyPositions.Add(pos);
            }
        }

        // --- Spawn traps using placement rules ---
        int trapsSpawned = 0;
        if (trapPrefab != null && trapTarget > 0)
        {
            var trapCells = TrapPlacement.PickTrapCells(
                validCells, roomBuilder, trapTarget,
                playerPos, enemyPositions, trapRules);

            foreach (var cell in trapCells)
            {
                Vector3 pos = roomBuilder.CellToWorld(cell);
                Instantiate(trapPrefab, pos, Quaternion.identity);
                trapsSpawned++;
            }
        }

        Debug.Log($"Spawned {enemiesSpawned} base enemies, {skittersSpawned} skitters, {trapsSpawned} traps");
        return enemiesSpawned + skittersSpawned;
    }

    void EmitSpawnFlash()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var waveObj = new GameObject("SpawnFlash");
        waveObj.transform.position = player.transform.position;
        waveObj.tag = "LightSource";

        var light = waveObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(1f, 0.95f, 0.8f);
        light.intensity = spawnFlashIntensity;
        light.pointLightOuterRadius = spawnFlashRadius;
        light.pointLightInnerRadius = spawnFlashRadius * 0.3f;
        light.pointLightOuterAngle = 360f;
        light.pointLightInnerAngle = 360f;
        light.shadowsEnabled = true;
        light.falloffIntensity = 0.5f;

        var collider = waveObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = spawnFlashRadius;

        var fader = waveObj.AddComponent<LightWaveFader>();
        fader.duration = spawnFlashDuration;
        fader.startIntensity = spawnFlashIntensity;
    }

    void HealPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var hp = player.GetComponent<PlayerHealth>();
        if (hp == null) return;

        int heal = Mathf.Max(healPerFloor - healDecay * (currentRoomIndex - 2), minHeal);
        hp.currentHealth = Mathf.Min(hp.currentHealth + heal, hp.maxHealth);
        StatusHUD.Instance?.UpdateHP(hp.currentHealth, hp.maxHealth);
        Debug.Log($"Healed {heal} HP (now {hp.currentHealth}/{hp.maxHealth})");
    }

    void HealPostBoss()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var hp = player.GetComponent<PlayerHealth>();
        if (hp == null) return;

        hp.currentHealth = Mathf.Min(hp.currentHealth + 2, hp.maxHealth);
        StatusHUD.Instance?.UpdateHP(hp.currentHealth, hp.maxHealth);
        Debug.Log($"Post-boss heal: +2 HP (now {hp.currentHealth}/{hp.maxHealth})");
    }

    // ── Shared helpers ──────────────────────────────────────────────────

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

        Vector2Int spawnCell = new Vector2Int(w / 2, h / 2);

        if (entryDirection == "left")
            spawnCell = FindSpawnNearEdge(w - 3, true);
        else if (entryDirection == "right")
            spawnCell = FindSpawnNearEdge(2, true);
        else if (entryDirection == "top")
            spawnCell = FindSpawnNearEdge(h - 3, false);
        else if (entryDirection == "bottom")
            spawnCell = FindSpawnNearEdge(2, false);
        else
        {
            // First room — pick a safe floor cell near center
            var floors = roomBuilder.FloorCells;
            if (floors.Count > 0)
                spawnCell = floors[Random.Range(0, floors.Count)];
        }

        Vector3 pos = roomBuilder.CellToWorld(spawnCell);
        player.transform.position = pos;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

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
                if (currentGrid[fixedCoord, y])
                    candidates.Add(new Vector2Int(fixedCoord, y));
        }
        else
        {
            for (int x = 2; x < w - 2; x++)
                if (currentGrid[x, fixedCoord])
                    candidates.Add(new Vector2Int(x, fixedCoord));
        }

        if (candidates.Count == 0)
            return new Vector2Int(w / 2, h / 2);

        return candidates[Random.Range(0, candidates.Count)];
    }

    void PlaceDoors(int w, int h)
    {
        var candidates = GetValidDoors(w, h);

        if (candidates.Count < 2)
        {
            Debug.LogWarning("Not enough door candidates!");
            return;
        }

        var doorA = candidates[Random.Range(0, candidates.Count)];

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
            for (int i = -half; i <= half; i++)
            {
                Vector2Int cell = new Vector2Int(door.pos.x, door.pos.y + i);
                roomBuilder.SetDoorCell(cell);
            }
        }
        else
        {
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
        foreach (var d in GameObject.FindGameObjectsWithTag("Door"))
            Destroy(d);
    }

    void ClearOldEntities()
    {
        foreach (var e in GameObject.FindGameObjectsWithTag("Enemy"))
            Destroy(e);
        foreach (var t in FindObjectsByType<Trap>(FindObjectsSortMode.None))
            Destroy(t.gameObject);
        foreach (var k in FindObjectsByType<Key>(FindObjectsSortMode.None))
            Destroy(k.gameObject);
    }

    class DoorCandidate
    {
        public Vector2Int pos;
        public string direction;
    }

    List<DoorCandidate> GetValidDoors(int w, int h)
    {
        List<DoorCandidate> valid = new List<DoorCandidate>();
        int size = 5;
        int half = size / 2;

        for (int x = 1; x < w - 1; x++)
        {
            for (int y = 1; y < h - 1; y++)
            {
                if (!currentGrid[x, y]) continue;

                if (x == 1 && CanPlaceVerticalDoor(x, y, half, w, h))
                    valid.Add(new DoorCandidate { pos = new Vector2Int(x, y), direction = "left" });

                if (x == w - 2 && CanPlaceVerticalDoor(x, y, half, w, h))
                    valid.Add(new DoorCandidate { pos = new Vector2Int(x, y), direction = "right" });

                if (y == 1 && CanPlaceHorizontalDoor(x, y, half, w, h))
                    valid.Add(new DoorCandidate { pos = new Vector2Int(x, y), direction = "bottom" });

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
            if (ny < 1 || ny >= h - 1) return false;
            if (!currentGrid[x, ny]) return false;
        }
        return true;
    }

    bool CanPlaceHorizontalDoor(int x, int y, int half, int w, int h)
    {
        for (int i = -half; i <= half; i++)
        {
            int nx = x + i;
            if (nx < 1 || nx >= w - 1) return false;
            if (!currentGrid[nx, y]) return false;
        }
        return true;
    }
}
