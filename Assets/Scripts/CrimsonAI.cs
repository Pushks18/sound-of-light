using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Boss AI for Crimson — the second boss.
///
/// Always vulnerable (no light requirement), compensated by high HP.
/// Chases the player, fires scattered bullets periodically, spawns
/// Type-1 (square red) minions at intervals, and poisons the player on contact.
///
/// Clone Phase: at least twice per fight, boss teleports to center, spawns 5
/// identical clones, all spread into a rotating circle. Player must find the
/// real boss by hitting it — clones vanish when boss is found. During formation
/// the boss is immune. In the circle, all are hittable.
/// </summary>
public class CrimsonAI : MonoBehaviour
{
    // ── Colour constants ────────────────────────────────────────────────────────
    static readonly Color BodyRed = new Color(0.85f, 0.08f, 0.08f, 1f);
    static readonly Color EyeRed  = new Color(0.32f, 0.00f, 0.00f, 1f);
    static readonly Color ArmRed  = new Color(0.80f, 0.07f, 0.07f, 1f);

    // ── Inspector ───────────────────────────────────────────────────────────────

    [Header("Stats")]
    [SerializeField] float maxHealth         = 75f;
    [SerializeField] float phase2HPThreshold = 30f;

    [Header("Movement")]
    [SerializeField] float moveSpeed       = 3.5f;
    [SerializeField] float phase2MoveSpeed = 5.5f;
    [SerializeField] float stopDistance    = 1.4f;
    [Tooltip("Boss stands still for this many seconds at the start of the fight before beginning to chase.")]
    [SerializeField] float moveDelay       = 7f;

    [Header("Auto-Start")]
    [Tooltip("Start fighting this many seconds after the scene loads, regardless of arena trigger.")]
    [SerializeField] float autoStartDelay = 2f;

    [Header("Scatter Bullets")]
    [SerializeField] float scatterInterval          = 12f;
    [SerializeField] float phase2ScatterInterval    = 8f;
    [SerializeField] int   scatterBulletCount       = 8;
    [SerializeField] int   phase2ScatterBulletCount = 14;
    [SerializeField] GameObject bulletPrefab;

    [Header("Minion Spawning")]
    [SerializeField] float spawnInterval       = 6f;
    [SerializeField] float phase2SpawnInterval = 6f;
    [SerializeField] int   maxMinions          = 4;
    [SerializeField] int   phase2MaxMinions    = 6;
    [SerializeField] GameObject enemyPrefab;

    [Header("Teleport")]
    [SerializeField] float teleportIntervalMin       = 8f;
    [SerializeField] float teleportIntervalMax       = 10f;
    [SerializeField] float phase2TeleportIntervalMin = 4f;
    [SerializeField] float phase2TeleportIntervalMax = 6f;
    [SerializeField] float teleportMinDistFromPlayer = 5f;

    [Header("Damage")]
    [SerializeField] float bulletDamage = 1f;
    [SerializeField] float slashDamage  = 3f;

    [Header("Clone Phase")]
    [SerializeField] int   cloneCount             = 5;
    [SerializeField] float circleRadius           = 4.5f;
    [SerializeField] float orbitSpeed             = 40f;        // deg/sec clockwise
    [SerializeField] float formationMoveDuration  = 1.5f;
    [SerializeField] float clonePhaseFirstTrigger = 35f;
    [SerializeField] float clonePhaseInterval     = 45f;
    [SerializeField] float cloneMinionInterval    = 12f;

    [Header("References")]
    [SerializeField] BossHealthBar healthBar;
    [SerializeField] BossIntroCam  bossIntroCam;

    [Header("Room Bounds")]
    [SerializeField] Vector2 roomCenter   = new Vector2(24f, 1f);
    [SerializeField] Vector2 roomHalfSize = new Vector2(11f, 11f);

    // ── State ───────────────────────────────────────────────────────────────────

    enum BossState { Dormant, InBattle, CloningFormation, CircleRotating, Dead }
    BossState state = BossState.Dormant;

    float health;
    float moveTimer;
    float scatterTimer;
    float spawnTimer;
    float teleportTimer;
    float clonePhaseTimer;
    float orbitAngle;
    float stunTimer;
    bool  immune;
    Coroutine hitFlashCoroutine;

    bool introScheduled = false;

    readonly List<CrimsonClone> activeClones = new List<CrimsonClone>();

    Transform   playerTransform;
    Rigidbody2D rb;

    // ── Lifecycle ───────────────────────────────────────────────────────────────

    void Awake()
    {
        if (DemoSequenceManager.IsActive) maxHealth = DemoSequenceManager.DemoBossMaxHealth;
        health = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        BuildVisuals();
    }

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        StartCoroutine(AutoStartAfterDelay());
    }

    IEnumerator AutoStartAfterDelay()
    {
        yield return new WaitForSeconds(autoStartDelay);
        if (state == BossState.Dormant)
            StartIntroSequence();
    }

    // ── Visual Construction ─────────────────────────────────────────────────────

    void BuildVisuals()
    {
        // Disable ALL Light2Ds in this hierarchy — but preserve any lights that
        // belong to the BossHealthBar or BossIntroCam subtrees (scene-placed UI).
        foreach (var l in GetComponentsInChildren<Light2D>(true))
        {
            if (healthBar   != null && l.transform.IsChildOf(healthBar.transform))   continue;
            if (bossIntroCam != null && l.transform.IsChildOf(bossIntroCam.transform)) continue;
            l.enabled = false;
        }

        // Deactivate legacy eye children from old prefab iterations.
        foreach (Transform child in transform)
            if (child.name == "EyeLeft" || child.name == "EyeRight")
                child.gameObject.SetActive(false);

        Sprite sq  = CreateSquareSprite();
        Material m = GetUnlitMat();

        var bodySR = GetComponent<SpriteRenderer>();
        if (bodySR != null)
        {
            bodySR.sprite       = sq;
            bodySR.color        = BodyRed;
            bodySR.material     = m;
            bodySR.sortingOrder = 5;
        }

        AddChildSprite("CrimsonEyeLeft",  new Vector3(-0.42f,  0.32f, -0.1f), new Vector3(0.48f, 0.48f, 1f), EyeRed, sq, m, 6);
        AddChildSprite("CrimsonEyeRight", new Vector3( 0.42f,  0.32f, -0.1f), new Vector3(0.48f, 0.48f, 1f), EyeRed, sq, m, 6);
        AddChildSprite("CrimsonArmLeft",  new Vector3(-1.30f, 0f, -0.05f),    new Vector3(0.70f, 0.40f, 1f), ArmRed, sq, m, 5);
        AddChildSprite("CrimsonArmRight", new Vector3( 1.30f, 0f, -0.05f),    new Vector3(0.70f, 0.40f, 1f), ArmRed, sq, m, 5);
    }

    // Builds the Crimson visual hierarchy on any root transform (used for clones too).
    static void BuildCrimsonVisuals(GameObject root)
    {
        Sprite sq  = CreateSquareSprite();
        Material m = GetUnlitMat();

        var bodySR = root.GetComponent<SpriteRenderer>();
        if (bodySR == null) bodySR = root.AddComponent<SpriteRenderer>();
        bodySR.sprite       = sq;
        bodySR.color        = BodyRed;
        bodySR.material     = m;
        bodySR.sortingOrder = 5;

        AddChildSpriteStatic(root.transform, "CrimsonEyeLeft",  new Vector3(-0.42f,  0.32f, -0.1f), new Vector3(0.48f, 0.48f, 1f), EyeRed, sq, m, 6);
        AddChildSpriteStatic(root.transform, "CrimsonEyeRight", new Vector3( 0.42f,  0.32f, -0.1f), new Vector3(0.48f, 0.48f, 1f), EyeRed, sq, m, 6);
        AddChildSpriteStatic(root.transform, "CrimsonArmLeft",  new Vector3(-1.30f, 0f, -0.05f),    new Vector3(0.70f, 0.40f, 1f), ArmRed, sq, m, 5);
        AddChildSpriteStatic(root.transform, "CrimsonArmRight", new Vector3( 1.30f, 0f, -0.05f),    new Vector3(0.70f, 0.40f, 1f), ArmRed, sq, m, 5);
    }

    void AddChildSprite(string n, Vector3 pos, Vector3 scale, Color color, Sprite sprite, Material mat, int order)
        => AddChildSpriteStatic(transform, n, pos, scale, color, sprite, mat, order);

    static void AddChildSpriteStatic(Transform parent, string n, Vector3 pos, Vector3 scale, Color color, Sprite sprite, Material mat, int order)
    {
        var go = new GameObject(n);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.color = color; sr.material = mat; sr.sortingOrder = order;
    }

    static Sprite CreateSquareSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    static Material _unlitMat;
    static Material GetUnlitMat()
    {
        if (_unlitMat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                      ?? Shader.Find("Sprites/Default");
            if (shader != null) _unlitMat = new Material(shader);
        }
        return _unlitMat;
    }

    // ── Public API ──────────────────────────────────────────────────────────────

    public bool TryScheduleIntro()
    {
        if (introScheduled) return false;
        introScheduled = true;
        return true;
    }

    public void TriggerIntroCamFade(float delay, float duration) { }

    public void StartIntroSequence()
    {
        if (state != BossState.Dormant) return;
        state = BossState.InBattle;

        if (healthBar != null) { healthBar.Initialize(maxHealth); healthBar.Show(); }

        moveTimer       = moveDelay;
        scatterTimer    = scatterInterval;
        spawnTimer      = spawnInterval;
        teleportTimer   = NextTeleportInterval();
        clonePhaseTimer = clonePhaseFirstTrigger;
    }

    public bool IsInBattle     => state == BossState.InBattle || state == BossState.CircleRotating;
    public bool IsInClonePhase => state == BossState.CircleRotating;

    public void Stun(float duration)
    {
        if (state == BossState.CircleRotating || state == BossState.Dormant || state == BossState.Dead) return;
        stunTimer = Mathf.Max(stunTimer, duration);
        if (rb != null) rb.linearVelocity = Vector2.zero;
        StartCoroutine(StunFlash());
    }

    IEnumerator StunFlash()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        // Pulse blue-white to distinguish from damage (red) flash
        Color stunCol = new Color(0.5f, 0.8f, 1f);
        float elapsed = 0f, dur = stunTimer;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            if (sr != null) sr.color = Color.Lerp(stunCol, BodyRed, elapsed / dur);
            yield return null;
        }
        if (sr != null) sr.color = BodyRed;
    }

    public void TakeDashDamage(float amount) => ApplyDamage(amount);

    // Called by CrimsonClone when all clones die without player finding boss.
    public void OnCloneKilled(CrimsonClone clone)
    {
        activeClones.Remove(clone);
        if (activeClones.Count == 0 && state == BossState.CircleRotating)
            EndClonePhase();
    }

    // Called by CrimsonClone when player's dash hits a clone.
    public void OnCloneDashHit(CrimsonClone clone) => clone.DieWithEffect();

    // Spawns a single minion at the given position (used by clones during circle).
    public void SpawnMinionAt(Vector2 pos)
    {
        if (enemyPrefab == null) return;
        Vector2 offset = Random.insideUnitCircle.normalized * 2.5f;
        var enemy = Instantiate(enemyPrefab, ClampToRoom(pos + offset), Quaternion.identity);
        enemy.GetComponent<EnemyAI>()?.ActivateHunt();
    }

    // ── FixedUpdate — movement ───────────────────────────────────────────────────

    void FixedUpdate()
    {
        if (state == BossState.CircleRotating)
        {
            orbitAngle -= orbitSpeed * Time.fixedDeltaTime;
            if (orbitAngle < -360f) orbitAngle += 360f;
            UpdateOrbitPositions();
            return;
        }

        if (state != BossState.InBattle) return;

        if (stunTimer > 0f)
        {
            stunTimer -= Time.fixedDeltaTime;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        if (moveTimer > 0f)
        {
            moveTimer -= Time.fixedDeltaTime;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        MoveTowardPlayer();
    }

    void MoveTowardPlayer()
    {
        if (playerTransform == null || rb == null) return;
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= stopDistance) { rb.linearVelocity = Vector2.zero; return; }
        float   speed = IsPhase2 ? phase2MoveSpeed : moveSpeed;
        Vector2 dir   = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * speed;
    }

    void UpdateOrbitPositions()
    {
        int total = activeClones.Count + 1;
        for (int i = 0; i < total; i++)
        {
            float   a   = (orbitAngle + i * 360f / total) * Mathf.Deg2Rad;
            Vector2 pos = (Vector2)roomCenter + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * circleRadius;
            if (i == 0)
            {
                if (rb != null) rb.MovePosition(pos);
            }
            else
            {
                var clone = activeClones[i - 1];
                if (clone != null) clone.transform.position = pos;
            }
        }
    }

    // ── Update — timers ─────────────────────────────────────────────────────────

    void Update()
    {
        if (state == BossState.InBattle)
        {
            if (stunTimer > 0f) return;

            scatterTimer -= Time.deltaTime;
            if (scatterTimer <= 0f)
            {
                FireScatter();
                scatterTimer = IsPhase2 ? phase2ScatterInterval : scatterInterval;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                TrySpawnMinions();
                spawnTimer = IsPhase2 ? phase2SpawnInterval : spawnInterval;
            }

            teleportTimer -= Time.deltaTime;
            if (teleportTimer <= 0f)
            {
                StartCoroutine(TeleportSequence());
                teleportTimer = NextTeleportInterval();
            }

            clonePhaseTimer -= Time.deltaTime;
            if (clonePhaseTimer <= 0f)
            {
                clonePhaseTimer = float.MaxValue; // prevent re-trigger until EndClonePhase resets it
                StopAllCoroutines();
                StartCoroutine(ClonePhase());
            }
        }

        if (state == BossState.CircleRotating)
        {
            // Boss + clones emit minions at low frequency during the circle.
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = cloneMinionInterval;
                // Spawn from a random entity in the circle.
                int idx = Random.Range(0, activeClones.Count + 1);
                Vector2 pos = idx == 0 ? (Vector2)transform.position : activeClones[idx - 1].transform.position;
                SpawnMinionAt(pos);
            }
        }
    }

    // ── Clone Phase ─────────────────────────────────────────────────────────────

    IEnumerator ClonePhase()
    {
        state  = BossState.CloningFormation;
        immune = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Teleport boss to room center
        yield return StartCoroutine(FadeAndMoveTo(roomCenter));

        // Rapid flash as warning
        yield return StartCoroutine(CloneFlash());

        // Spawn clones at center
        activeClones.Clear();
        for (int i = 0; i < cloneCount; i++)
            activeClones.Add(SpawnClone((Vector2)roomCenter));

        // Calculate the spread-out circle targets (boss = slot 0)
        int      total   = cloneCount + 1;
        orbitAngle       = 90f;
        var targets      = new Vector2[total];
        for (int i = 0; i < total; i++)
        {
            float a  = (orbitAngle + i * 360f / total) * Mathf.Deg2Rad;
            targets[i] = (Vector2)roomCenter + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * circleRadius;
        }

        // Animate spreading outward
        float   elapsed = 0f;
        while (elapsed < formationMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / formationMoveDuration);

            if (rb != null) rb.MovePosition(Vector2.Lerp(roomCenter, targets[0], t));

            for (int i = 0; i < activeClones.Count; i++)
                if (activeClones[i] != null)
                    activeClones[i].transform.position = Vector2.Lerp(roomCenter, targets[i + 1], t);

            yield return null;
        }

        // Snap final positions
        transform.position = targets[0];
        for (int i = 0; i < activeClones.Count; i++)
            if (activeClones[i] != null)
                activeClones[i].transform.position = targets[i + 1];

        immune             = false;
        state              = BossState.CircleRotating;
        spawnTimer         = cloneMinionInterval;
    }

    IEnumerator FadeAndMoveTo(Vector2 destination)
    {
        var srs  = GetComponentsInChildren<SpriteRenderer>(true);
        var orig = new Color[srs.Length];
        for (int i = 0; i < srs.Length; i++) if (srs[i] != null) orig[i] = srs[i].color;

        float dur = 0.2f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            for (int i = 0; i < srs.Length; i++)
                if (srs[i] != null) { var c = orig[i]; c.a = 1f - t; srs[i].color = c; }
            yield return null;
        }

        if (rb != null) rb.linearVelocity = Vector2.zero;
        transform.position = destination;

        elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            for (int i = 0; i < srs.Length; i++)
                if (srs[i] != null) { var c = orig[i]; c.a = t; srs[i].color = c; }
            yield return null;
        }

        for (int i = 0; i < srs.Length; i++) if (srs[i] != null) srs[i].color = orig[i];
    }

    IEnumerator CloneFlash()
    {
        var srs  = GetComponentsInChildren<SpriteRenderer>(true);
        var orig = new Color[srs.Length];
        for (int i = 0; i < srs.Length; i++) if (srs[i] != null) orig[i] = srs[i].color;

        for (int f = 0; f < 5; f++)
        {
            foreach (var sr in srs) if (sr != null) sr.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            for (int i = 0; i < srs.Length; i++) if (srs[i] != null) srs[i].color = orig[i];
            yield return new WaitForSeconds(0.08f);
        }
    }

    CrimsonClone SpawnClone(Vector2 pos)
    {
        var go = new GameObject("CrimsonClone");
        go.transform.position   = pos;
        go.transform.localScale = transform.localScale;

        BuildCrimsonVisuals(go);

        // Collider matching boss size
        var bossColl  = GetComponent<BoxCollider2D>();
        var cloneColl = go.AddComponent<BoxCollider2D>();
        cloneColl.isTrigger = true;
        if (bossColl != null) cloneColl.size = bossColl.size;

        var clone = go.AddComponent<CrimsonClone>();
        clone.Init(this, cloneMinionInterval);
        return clone;
    }

    void KillAllClonesWithEffect()
    {
        // Iterate over a copy so removal during iteration is safe.
        var snapshot = new List<CrimsonClone>(activeClones);
        activeClones.Clear();
        foreach (var clone in snapshot)
            if (clone != null)
                clone.DieWithEffect();
    }

    void EndClonePhase()
    {
        state           = BossState.InBattle;
        moveTimer       = 0f;
        scatterTimer    = IsPhase2 ? phase2ScatterInterval : scatterInterval;
        spawnTimer      = IsPhase2 ? phase2SpawnInterval   : spawnInterval;
        teleportTimer   = NextTeleportInterval();
        clonePhaseTimer = clonePhaseInterval;
    }

    // ── Scatter Attack ──────────────────────────────────────────────────────────

    void FireScatter()
    {
        if (bulletPrefab == null) return;
        int   count     = IsPhase2 ? phase2ScatterBulletCount : scatterBulletCount;
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float rotation = angleStep * i - 90f;
            var bullet = Instantiate(bulletPrefab, transform.position, Quaternion.Euler(0f, 0f, rotation));
            var sr = bullet.GetComponent<SpriteRenderer>();
            if (sr != null) { sr.material = GetUnlitMat(); sr.color = new Color(1f, 0.25f, 0.1f); sr.sortingOrder = 12; }
            // No extra Light2D added — stacking 8-14 lights at spawn position washes the boss white.
        }
    }

    // ── Minion Spawning ─────────────────────────────────────────────────────────

    void TrySpawnMinions()
    {
        if (enemyPrefab == null) return;
        var alive = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        int limit  = IsPhase2 ? phase2MaxMinions : maxMinions;
        if (alive.Length >= limit) return;
        int count = IsPhase2 ? 2 : 1;
        for (int i = 0; i < count && alive.Length + i < limit; i++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * 2.5f;
            Vector2 pos    = ClampToRoom((Vector2)transform.position + offset);
            var enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
            enemy.GetComponent<EnemyAI>()?.ActivateHunt();
        }
    }

    Vector2 ClampToRoom(Vector2 pos)
    {
        const float margin = 1f;
        return new Vector2(
            Mathf.Clamp(pos.x, roomCenter.x - roomHalfSize.x + margin, roomCenter.x + roomHalfSize.x - margin),
            Mathf.Clamp(pos.y, roomCenter.y - roomHalfSize.y + margin, roomCenter.y + roomHalfSize.y - margin));
    }

    // ── Teleport ────────────────────────────────────────────────────────────────

    float NextTeleportInterval()
    {
        return IsPhase2
            ? Random.Range(phase2TeleportIntervalMin, phase2TeleportIntervalMax)
            : Random.Range(teleportIntervalMin,       teleportIntervalMax);
    }

    IEnumerator TeleportSequence()
    {
        var renderers   = GetComponentsInChildren<SpriteRenderer>(true);
        float fadeOut   = 0.15f, elapsed = 0f;
        var   startCols = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) startCols[i] = renderers[i].color;

        while (elapsed < fadeOut)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOut;
            for (int i = 0; i < renderers.Length; i++)
            { if (renderers[i] == null) continue; var c = startCols[i]; c.a = Mathf.Lerp(1f, 0f, t); renderers[i].color = c; }
            yield return null;
        }

        if (rb != null) rb.linearVelocity = Vector2.zero;
        transform.position = FindTeleportPosition();

        float fadeIn = 0.15f;
        elapsed = 0f;
        while (elapsed < fadeIn)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeIn;
            for (int i = 0; i < renderers.Length; i++)
            { if (renderers[i] == null) continue; var c = startCols[i]; c.a = Mathf.Lerp(0f, 1f, t); renderers[i].color = c; }
            yield return null;
        }

        for (int i = 0; i < renderers.Length; i++)
        { if (renderers[i] == null) continue; var c = startCols[i]; c.a = 1f; renderers[i].color = c; }
    }

    Vector2 FindTeleportPosition()
    {
        Vector2 playerPos = playerTransform != null ? (Vector2)playerTransform.position : roomCenter;
        Vector2 inset = roomHalfSize * 0.85f;
        Vector2[] corners = new Vector2[]
        {
            roomCenter + new Vector2(-inset.x, -inset.y),
            roomCenter + new Vector2( inset.x, -inset.y),
            roomCenter + new Vector2(-inset.x,  inset.y),
            roomCenter + new Vector2( inset.x,  inset.y),
        };
        for (int i = corners.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (corners[i], corners[j]) = (corners[j], corners[i]);
        }
        foreach (var corner in corners)
            if (Vector2.Distance(corner, playerPos) >= teleportMinDistFromPlayer)
                return corner;
        Vector2 away = (roomCenter - playerPos).normalized;
        if (away == Vector2.zero) away = Vector2.up;
        return roomCenter + away * inset;
    }

    // ── Trigger Callbacks ───────────────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        if (state == BossState.Dormant || state == BossState.Dead) return;
        if (immune) return;
        if (state == BossState.CloningFormation) return;

        if (other.CompareTag("Bullet"))
        {
            Destroy(other.gameObject);
            ApplyDamage(bulletDamage);
            return;
        }

        if (other.CompareTag("LightSource") && other.GetComponent<SlashBulletDeflector>() != null)
            ApplyDamage(slashDamage);
    }

    // ── Damage ──────────────────────────────────────────────────────────────────

    void ApplyDamage(float amount)
    {
        if (state == BossState.Dormant || state == BossState.Dead) return;
        if (immune) return;

        // Real boss found during circle — kill all clones, resume fight.
        if (state == BossState.CircleRotating)
        {
            KillAllClonesWithEffect();
            EndClonePhase();
        }

        health -= amount;
        health  = Mathf.Max(health, 0f);

        if (healthBar != null) healthBar.SetHealth(health);
        DamageNumber.Spawn(Mathf.CeilToInt(amount), transform.position);
        if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
        hitFlashCoroutine = StartCoroutine(HitFlash());

        if (health <= 0f)
        {
            StopAllCoroutines();
            KillAllClonesWithEffect();
            StartCoroutine(DeathSequence());
        }
    }

    IEnumerator HitFlash()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        sr.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        // Always restore to the known body colour — never read sr.color as "orig"
        // because a concurrent flash would have left it white.
        if (sr != null) sr.color = BodyRed;
        hitFlashCoroutine = null;
    }

    bool IsPhase2 => health <= phase2HPThreshold;

    // ── Death ───────────────────────────────────────────────────────────────────

    IEnumerator DeathSequence()
    {
        if (state == BossState.Dead) yield break;
        state = BossState.Dead;

        if (healthBar != null) healthBar.Hide();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        var renderers   = GetComponentsInChildren<SpriteRenderer>(true);
        var startColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) startColors[i] = renderers[i].color;

        float elapsed = 0f;
        const float duration = 1.8f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                var c = startColors[i]; c.a = Mathf.Lerp(1f, 0f, t); renderers[i].color = c;
            }
            yield return null;
        }

        yield return new WaitForSeconds(0.4f);

        if (bossIntroCam != null && playerTransform != null)
            yield return StartCoroutine(bossIntroCam.PanBackToPlayer(playerTransform));

        GameManager.Instance?.BossDefeated();
        Destroy(gameObject);
    }
}

// ── CrimsonClone ─────────────────────────────────────────────────────────────

public class CrimsonClone : MonoBehaviour
{
    CrimsonAI boss;
    float     minionTimer;
    float     minionInterval;
    bool      dying;

    public void Init(CrimsonAI boss, float minionInterval)
    {
        this.boss           = boss;
        this.minionInterval = minionInterval;
        minionTimer         = minionInterval * Random.Range(0.5f, 1f); // stagger initial spawn
    }

    void Update()
    {
        if (dying) return;
        minionTimer -= Time.deltaTime;
        if (minionTimer <= 0f)
        {
            minionTimer = minionInterval;
            boss?.SpawnMinionAt(transform.position);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (dying) return;

        if (other.CompareTag("Bullet"))
        {
            Destroy(other.gameObject);
            DieWithEffect();
            return;
        }

        if (other.CompareTag("LightSource") && other.GetComponent<SlashBulletDeflector>() != null)
            DieWithEffect();
    }

    // Called by PlayerDash when a dash hits this clone.
    public void TakeDashHit()
    {
        if (!dying) DieWithEffect();
    }

    public void DieWithEffect()
    {
        if (dying) return;
        dying = true;
        boss?.OnCloneKilled(this);
        StartCoroutine(DeathBurst());
    }

    IEnumerator DeathBurst()
    {
        // Disable collider immediately so no double-hits.
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        var srs      = GetComponentsInChildren<SpriteRenderer>(true);
        var origCols = new Color[srs.Length];
        for (int i = 0; i < srs.Length; i++) if (srs[i] != null) origCols[i] = srs[i].color;

        float elapsed = 0f, dur = 0.45f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;

            // Expand outward + flash orange-red then fade
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 2.8f, t);
            for (int i = 0; i < srs.Length; i++)
            {
                if (srs[i] == null) continue;
                Color c = origCols[i];
                c.r = Mathf.Lerp(c.r, 1f,   t);
                c.g = Mathf.Lerp(c.g, 0.4f, t);
                c.b = Mathf.Lerp(c.b, 0f,   t);
                c.a = Mathf.Lerp(1f,  0f,   t);
                srs[i].color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}
