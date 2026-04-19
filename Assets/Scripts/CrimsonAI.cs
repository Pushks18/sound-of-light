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
/// Auto-starts the fight after autoStartDelay seconds so the fight begins
/// even if the arena trigger isn't crossed (e.g. when testing directly in
/// the NewBoss1Scene).
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
    [SerializeField] float scatterInterval       = 12f;
    [SerializeField] float phase2ScatterInterval = 8f;
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

    [Header("References")]
    [SerializeField] BossHealthBar healthBar;
    [SerializeField] BossIntroCam  bossIntroCam;

    [Header("Room Bounds")]
    [SerializeField] Vector2 roomCenter   = new Vector2(24f, 1f);
    [SerializeField] Vector2 roomHalfSize = new Vector2(11f, 11f);

    // ── State ───────────────────────────────────────────────────────────────────

    enum BossState { Dormant, InBattle, Dead }
    BossState state = BossState.Dormant;

    float health;
    float moveTimer;         // counts down; boss only chases once this hits 0
    float scatterTimer;
    float spawnTimer;
    float teleportTimer;

    bool introScheduled = false;

    Transform   playerTransform;
    Rigidbody2D rb;

    // ── Lifecycle ───────────────────────────────────────────────────────────────

    void Awake()
    {
        health = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        BuildVisuals();
    }

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Auto-start so the boss fights even without the arena trigger.
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
        Sprite sq  = CreateSquareSprite();
        Material m = GetUnlitMat();

        // Deactivate legacy Vesper eye children
        foreach (Transform child in transform)
        {
            if (child.name == "EyeLeft" || child.name == "EyeRight")
                child.gameObject.SetActive(false);
        }

        // Body — existing SpriteRenderer on this GameObject
        var bodySR = GetComponent<SpriteRenderer>();
        if (bodySR != null)
        {
            bodySR.sprite       = sq;
            bodySR.color        = BodyRed;
            bodySR.material     = m;
            bodySR.sortingOrder = 5;
        }

        // Eyes — dark red squares in the upper half
        AddChildSprite("CrimsonEyeLeft",  new Vector3(-0.42f,  0.32f, -0.1f), new Vector3(0.48f, 0.48f, 1f), EyeRed, sq, m, 6);
        AddChildSprite("CrimsonEyeRight", new Vector3( 0.42f,  0.32f, -0.1f), new Vector3(0.48f, 0.48f, 1f), EyeRed, sq, m, 6);

        // Arms — wide flat rectangles on each side
        AddChildSprite("CrimsonArmLeft",  new Vector3(-1.30f, 0f, -0.05f), new Vector3(0.70f, 0.40f, 1f), ArmRed, sq, m, 5);
        AddChildSprite("CrimsonArmRight", new Vector3( 1.30f, 0f, -0.05f), new Vector3(0.70f, 0.40f, 1f), ArmRed, sq, m, 5);
    }

    void AddChildSprite(string n, Vector3 pos, Vector3 scale, Color color, Sprite sprite, Material mat, int order)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.color        = color;
        sr.material     = mat;
        sr.sortingOrder = order;
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
            // Prefer the explicit unlit sprite shader; fall back to legacy Sprites/Default
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                      ?? Shader.Find("Sprites/Default");
            if (shader != null) _unlitMat = new Material(shader);
        }
        return _unlitMat;
    }

    // ── Public API (called by CrimsonArenaTrigger) ──────────────────────────────

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

        if (healthBar != null)
        {
            healthBar.Initialize(maxHealth);
            healthBar.Show();
        }

        moveTimer     = moveDelay;
        scatterTimer  = scatterInterval;
        spawnTimer    = spawnInterval;
        teleportTimer = NextTeleportInterval();
    }

    public bool IsInBattle => state == BossState.InBattle;

    // ── FixedUpdate — movement ───────────────────────────────────────────────────

    void FixedUpdate()
    {
        if (state != BossState.InBattle) return;

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
        if (dist <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float  speed = IsPhase2 ? phase2MoveSpeed : moveSpeed;
        Vector2 dir  = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * speed;
    }

    // ── Update — timers ─────────────────────────────────────────────────────────

    void Update()
    {
        if (state != BossState.InBattle) return;

        // Scatter attack
        scatterTimer -= Time.deltaTime;
        if (scatterTimer <= 0f)
        {
            FireScatter();
            scatterTimer = IsPhase2 ? phase2ScatterInterval : scatterInterval;
        }

        // Minion spawning
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            TrySpawnMinions();
            spawnTimer = IsPhase2 ? phase2SpawnInterval : spawnInterval;
        }

        // Teleport
        teleportTimer -= Time.deltaTime;
        if (teleportTimer <= 0f)
        {
            StartCoroutine(TeleportSequence());
            teleportTimer = NextTeleportInterval();
        }
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

            // Force unlit rendering so bullets are visible in the dark scene
            var sr = bullet.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.material     = GetUnlitMat();
                sr.color        = new Color(1f, 0.25f, 0.1f);
                sr.sortingOrder = 12;
            }

            // Add a glow so bullets are obvious
            var glow = bullet.AddComponent<Light2D>();
            glow.lightType               = Light2D.LightType.Point;
            glow.color                   = new Color(1f, 0.2f, 0.1f);
            glow.intensity               = 1.2f;
            glow.pointLightOuterRadius   = 0.9f;
            glow.pointLightInnerRadius   = 0.2f;
            glow.shadowsEnabled          = false;
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
            Vector2 pos   = GetMinionSpawnPos();
            var     enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
            enemy.GetComponent<EnemyAI>()?.ActivateHunt();
        }
    }

    Vector2 GetMinionSpawnPos()
    {
        // Spawn at the boss's current position (enemies emerge from inside Crimson)
        return (Vector2)transform.position;
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
        // Brief fade-out
        var renderers   = GetComponentsInChildren<SpriteRenderer>(true);
        float fadeOut   = 0.15f;
        float elapsed   = 0f;
        var   startCols = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) startCols[i] = renderers[i].color;

        while (elapsed < fadeOut)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOut;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                var c = startCols[i]; c.a = Mathf.Lerp(1f, 0f, t); renderers[i].color = c;
            }
            yield return null;
        }

        // Move to new position
        if (rb != null) rb.linearVelocity = Vector2.zero;
        transform.position = FindTeleportPosition();

        // Brief fade-in
        float fadeIn = 0.15f;
        elapsed = 0f;
        while (elapsed < fadeIn)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeIn;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                var c = startCols[i]; c.a = Mathf.Lerp(0f, 1f, t); renderers[i].color = c;
            }
            yield return null;
        }

        // Restore full alpha
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            var c = startCols[i]; c.a = 1f; renderers[i].color = c;
        }
    }

    Vector2 FindTeleportPosition()
    {
        Vector2 playerPos = playerTransform != null ? (Vector2)playerTransform.position : roomCenter;

        // Build corner candidates (inset slightly from walls)
        Vector2 inset = roomHalfSize * 0.85f;
        Vector2[] corners = new Vector2[]
        {
            roomCenter + new Vector2(-inset.x, -inset.y),
            roomCenter + new Vector2( inset.x, -inset.y),
            roomCenter + new Vector2(-inset.x,  inset.y),
            roomCenter + new Vector2( inset.x,  inset.y),
        };

        // Shuffle corners
        for (int i = corners.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (corners[i], corners[j]) = (corners[j], corners[i]);
        }

        // Pick first corner that is far enough from player
        foreach (var corner in corners)
        {
            if (Vector2.Distance(corner, playerPos) >= teleportMinDistFromPlayer)
                return corner;
        }

        // Fallback: opposite side of room from player
        Vector2 away = (roomCenter - playerPos).normalized;
        if (away == Vector2.zero) away = Vector2.up;
        return roomCenter + away * inset;
    }

    // ── Trigger Callbacks ───────────────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        if (state != BossState.InBattle) return;

        if (other.CompareTag("Bullet"))
        {
            Destroy(other.gameObject);
            ApplyDamage(bulletDamage);
            return;
        }

        if (other.CompareTag("LightSource") && other.GetComponent<SlashBulletDeflector>() != null)
        {
            ApplyDamage(slashDamage);
            return;
        }
    }

    // ── Damage ──────────────────────────────────────────────────────────────────

    void ApplyDamage(float amount)
    {
        if (state != BossState.InBattle) return;

        health -= amount;
        health  = Mathf.Max(health, 0f);

        if (healthBar != null) healthBar.SetHealth(health);

        DamageNumber.Spawn(Mathf.CeilToInt(amount), transform.position);
        StartCoroutine(HitFlash());

        if (health <= 0f)
        {
            StopAllCoroutines();
            StartCoroutine(DeathSequence());
        }
    }

    IEnumerator HitFlash()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        Color orig = sr.color;
        sr.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        if (sr != null) sr.color = orig;
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
        for (int i = 0; i < renderers.Length; i++)
            startColors[i] = renderers[i].color;

        float elapsed = 0f;
        const float duration = 1.8f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                var c = startColors[i];
                c.a = Mathf.Lerp(1f, 0f, t);
                renderers[i].color = c;
            }
            yield return null;
        }

        yield return new WaitForSeconds(0.4f);
        GameManager.Instance?.BossDefeated();
        Destroy(gameObject);
    }
}
