using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Drop this on a new empty GameObject in NewTut scene.
/// Wire up roomBuilder (the Grid's TilemapRoomBuilder), enemyPrefab, and trapPrefab.
/// On Play it carves the exact tutorial layout, places all lights, enemies, and traps,
/// and creates the room-entry triggers for TutorialManager automatically.
/// </summary>
public class TutorialLayoutGenerator : MonoBehaviour
{
    // ── References ────────────────────────────────────────────────────────
    [Header("Required References")]
    public TilemapRoomBuilder roomBuilder;
    public GameObject enemyPrefab;
    public GameObject trapPrefab;

    // ── Light tweaks ──────────────────────────────────────────────────────
    [Header("Light Appearance  (matches idle auto-flash style)")]
    public float lightRadius     = 6f;
    public float lightMinIntensity = 0.6f;
    public float lightMaxIntensity = 1.5f;
    public float lightPulseSpeed   = 0.8f;                           // cycles/sec — slow breath
    public Color lightColor      = new Color(0.8f, 0.85f, 1f);      // cool blue-white, same as idle flash
    public float minLightSpacing = 4f;

    [Header("Light Counts per Room")]
    public int room0Lights = 7;   // start room
    public int room2Lights = 4;   // shoot room
    public int room3Lights = 3;   // trap room (enough to see traps)
    public int room4Lights = 7;   // final room

    // ═════════════════════════════════════════════════════════════════════
    //  LAYOUT  (all values in tilemap-cell space, 1 cell = 1 world unit)
    //
    //  Top-level path:
    //    Room0 ──right──▶ Room1 ──right──▶ Room2
    //                                        │ up
    //                                       Room3
    //                                        │ left
    //    Room4 ◀──────────────────────────────┘
    // ═════════════════════════════════════════════════════════════════════
    const int GRID_W = 72;
    const int GRID_H = 54;

    // Rooms: (xMin, yMin, width, height)
    static readonly (int x, int y, int w, int h) Room0 = ( 2,  2, 22, 16);  // start
    static readonly (int x, int y, int w, int h) Room1 = (30,  2,  8, 22);  // dark / slash
    static readonly (int x, int y, int w, int h) Room2 = (44,  4, 20, 14);  // shoot / ambush
    static readonly (int x, int y, int w, int h) Room3 = (50, 26, 18, 12);  // trap / dash
    static readonly (int x, int y, int w, int h) Room4 = ( 4, 32, 24, 18);  // final

    // Corridors connecting the rooms
    static readonly (int x, int y, int w, int h) Corr01 = (24,  8,  6,  4); // Room0 → Room1
    static readonly (int x, int y, int w, int h) Corr12 = (38,  8,  6,  4); // Room1 → Room2
    static readonly (int x, int y, int w, int h) Corr23 = (57, 18,  4,  8); // Room2 ↑ Room3
    static readonly (int x, int y, int w, int h) Corr34 = (28, 35, 22,  4); // Room3 ← Room4

    // Fixed enemy tile positions
    static readonly Vector2Int E_Dark1  = new Vector2Int(34, 20);  // Room1 top  (attacks front)
    static readonly Vector2Int E_Dark2  = new Vector2Int(34,  4);  // Room1 bot  (attacks behind)
    static readonly Vector2Int E_Shoot1 = new Vector2Int(48, 11);  // Room2
    static readonly Vector2Int E_Shoot2 = new Vector2Int(54,  7);  // Room2
    static readonly Vector2Int E_Shoot3 = new Vector2Int(61, 14);  // Room2
    static readonly Vector2Int E_Trap   = new Vector2Int(55, 28);  // Room3 entrance
    static readonly Vector2Int E_Final  = new Vector2Int(10, 44);  // Room4 far end

    // Trap row across Room3 — player must dash past these
    static readonly Vector2Int[] TrapRow =
    {
        new Vector2Int(52, 32), new Vector2Int(55, 32), new Vector2Int(58, 32),
        new Vector2Int(61, 32), new Vector2Int(64, 32), new Vector2Int(67, 32),
    };

    // Player spawn
    static readonly Vector2Int PlayerSpawn = new Vector2Int(13, 10);

    // ─────────────────────────────────────────────────────────────────────
    void Start() => StartCoroutine(Build());

    IEnumerator Build()
    {
        // 1 ── Carve grid
        bool[,] grid = new bool[GRID_W, GRID_H];
        Carve(grid, Room0);  Carve(grid, Room1);  Carve(grid, Room2);
        Carve(grid, Room3);  Carve(grid, Room4);
        Carve(grid, Corr01); Carve(grid, Corr12);
        Carve(grid, Corr23); Carve(grid, Corr34);

        // 2 ── Build tilemap, then wait one physics frame for
        //      TilemapCollider2D to finish rebuilding wall colliders
        roomBuilder.BuildFromGrid(grid, GRID_W, GRID_H);
        yield return new WaitForFixedUpdate();

        // 3 ── Scatter lights (randomly inside each room's bounds)
        ScatterLights(Room0, room0Lights);
        ScatterLights(Room2, room2Lights);
        ScatterLights(Room3, room3Lights);
        ScatterLights(Room4, room4Lights);
        // Room1 stays DARK — TutorialManager dims global light on entry

        // 4 ── Spawn enemies
        //   Room1: activated naturally by player's L-flash (normal EnemyAI)
        SpawnEnemy(E_Dark1);
        SpawnEnemy(E_Dark2);

        //   Room2: immediately ambush — need EnemyActivator
        var s1 = SpawnEnemy(E_Shoot1);
        var s2 = SpawnEnemy(E_Shoot2);
        var s3 = SpawnEnemy(E_Shoot3);
        CreateActivator(Corr12, EnemiesOf(s1, s2, s3));

        //   Room3: one enemy at entrance, activates immediately
        var te = SpawnEnemy(E_Trap);
        CreateActivator(Corr23, EnemiesOf(te));

        //   Room4: one enemy, dormant (activates on flash — final test)
        SpawnEnemy(E_Final);

        // 5 ── Spawn traps (armed from start so player can see them)
        foreach (var cell in TrapRow)
        {
            var trapGO = Instantiate(trapPrefab, W(cell), Quaternion.identity);
            var trap = trapGO.GetComponent<Trap>();
            if (trap != null) trap.startArmed = true;
        }

        // 6 ── Create room-entry triggers for TutorialManager
        //   (placed in mid-corridor so they fire just as player enters each room)
        MakeRoomTrigger(1, MidPoint(Corr01), new Vector2(4, Corr01.h));
        MakeRoomTrigger(2, MidPoint(Corr12), new Vector2(4, Corr12.h));
        MakeRoomTrigger(3, MidPoint(Corr23), new Vector2(Corr23.w, 4));
        MakeRoomTrigger(4, MidPoint(Corr34), new Vector2(Corr34.w, 4));

        // 7 ── Place player at start room centre
        PlacePlayer();
    }

    // ── Grid carving ──────────────────────────────────────────────────────
    void Carve(bool[,] g, (int x, int y, int w, int h) r)
    {
        for (int x = r.x; x < r.x + r.w; x++)
        for (int y = r.y; y < r.y + r.h; y++)
            if (x >= 0 && x < GRID_W && y >= 0 && y < GRID_H)
                g[x, y] = true;
    }

    // ── Random light scatter ──────────────────────────────────────────────
    void ScatterLights((int x, int y, int w, int h) room, int count)
    {
        int margin   = 2;
        int xMin = room.x + margin,  xMax = room.x + room.w - margin;
        int yMin = room.y + margin,  yMax = room.y + room.h - margin;

        var placed = new List<Vector2>();
        int attempts = count * 25;

        for (int i = 0; i < count && attempts > 0; attempts--)
        {
            int tx = Random.Range(xMin, xMax);
            int ty = Random.Range(yMin, yMax);
            Vector2 candidate = new Vector2(tx, ty);

            bool tooClose = false;
            foreach (var p in placed)
                if (Vector2.Distance(p, candidate) < minLightSpacing)
                { tooClose = true; break; }

            if (tooClose) continue;

            SpawnLight(roomBuilder.CellToWorld(new Vector2Int(tx, ty)));
            placed.Add(candidate);
            i++;
        }
    }

    void SpawnLight(Vector3 pos)
    {
        var go = new GameObject("TutLight");
        go.transform.position = pos;
        go.tag = "LightSource";

        var l = go.AddComponent<Light2D>();
        l.lightType             = Light2D.LightType.Point;
        l.color                 = lightColor;
        l.intensity             = lightMaxIntensity;   // pulse script takes over immediately
        l.pointLightOuterRadius = lightRadius;
        l.pointLightInnerRadius = lightRadius * 0.25f;
        l.pointLightOuterAngle  = 360f;
        l.pointLightInnerAngle  = 360f;
        l.shadowsEnabled        = true;
        l.falloffIntensity      = 0.5f;

        // Trigger collider — enemies activate inside the light pool
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = lightRadius;

        // Breathing pulse — random phase so each light pulses independently
        var pulse = go.AddComponent<TutorialLightPulse>();
        pulse.minIntensity = lightMinIntensity;
        pulse.maxIntensity = lightMaxIntensity;
        pulse.speed        = lightPulseSpeed;
        pulse.phase        = Random.Range(0f, Mathf.PI * 2f);
    }

    // ── Enemy spawning ────────────────────────────────────────────────────
    GameObject SpawnEnemy(Vector2Int cell)
    {
        if (enemyPrefab == null) return null;
        return Instantiate(enemyPrefab, W(cell), Quaternion.identity);
    }

    EnemyAI[] EnemiesOf(params GameObject[] gos)
    {
        var list = new List<EnemyAI>();
        foreach (var go in gos)
            if (go != null && go.TryGetComponent<EnemyAI>(out var ai))
                list.Add(ai);
        return list.ToArray();
    }

    // ── Enemy activator trigger ───────────────────────────────────────────
    void CreateActivator((int x, int y, int w, int h) corrRect, EnemyAI[] enemies)
    {
        var go = new GameObject("EnemyActivator");
        go.transform.position = MidPoint(corrRect);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(corrRect.w, corrRect.h);

        var act = go.AddComponent<TutorialEnemyActivator>();
        act.enemies = enemies;
    }

    // ── Room-entry trigger for TutorialManager ────────────────────────────
    void MakeRoomTrigger(int roomIndex, Vector3 centre, Vector2 size)
    {
        var go = new GameObject($"RoomTrigger_{roomIndex}");
        go.transform.position = centre;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = size;

        var t = go.AddComponent<TutorialRoomTrigger>();
        t.roomIndex = roomIndex;
    }

    // ── Player placement ──────────────────────────────────────────────────
    void PlacePlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Vector3 spawnPos = W(PlayerSpawn);
        player.transform.position = spawnPos;

        if (player.TryGetComponent<Rigidbody2D>(out var rb))
            rb.linearVelocity = Vector2.zero;

        // Make player immortal for the tutorial
        if (!player.TryGetComponent<TutorialInvincibility>(out _))
            player.AddComponent<TutorialInvincibility>();

        // Assign + snap the camera so it doesn't lerp from a far-away position
        var cam = Camera.main;
        if (cam != null)
        {
            if (cam.TryGetComponent<CameraFollow>(out var cf))
                cf.target = player.transform;

            cam.transform.position = new Vector3(spawnPos.x, spawnPos.y, -10f);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    // Tilemap cell → world position
    Vector3 W(Vector2Int cell) => roomBuilder.CellToWorld(cell);

    // Centre world position of a rect (in tile space)
    Vector3 MidPoint((int x, int y, int w, int h) r)
        => W(new Vector2Int(r.x + r.w / 2, r.y + r.h / 2));

    // Draw all rooms and corridors in Scene view for easy inspection
    void OnDrawGizmos()
    {
        DrawGizmoRect(Room0,  Color.cyan);
        DrawGizmoRect(Room1,  Color.gray);
        DrawGizmoRect(Room2,  Color.yellow);
        DrawGizmoRect(Room3,  Color.red);
        DrawGizmoRect(Room4,  Color.green);
        DrawGizmoRect(Corr01, Color.white);
        DrawGizmoRect(Corr12, Color.white);
        DrawGizmoRect(Corr23, Color.white);
        DrawGizmoRect(Corr34, Color.white);
    }

    void DrawGizmoRect((int x, int y, int w, int h) r, Color c)
    {
        Gizmos.color = c;
        Vector3 center = new Vector3(r.x + r.w * 0.5f, r.y + r.h * 0.5f, 0f);
        Vector3 size   = new Vector3(r.w, r.h, 0.1f);
        Gizmos.DrawWireCube(center, size);
    }
}
