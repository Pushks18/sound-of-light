using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Boss AI for Umbra — NewBoss2Scene.
///
/// COMBAT:
///   Close  → large slash, phase1: 2-3 s interval / phase2: 0.5 s multi-slash.
///   Far    → rapid bullets (0.1 s cooldown).
///   Idle   → periodic multi-directional bullet burst every ~5 s.
///   Berserk→ every 5 s: chases player at high speed, zig-zags, contact dmg,
///            omni-bursts on each zig turn. Lasts 7-8 s.
///   Phase2 → periodic full-arena SWEEP: grey overlay + screen warning,
///            player must DASH to survive.
/// </summary>
public class ShadowBossAI : MonoBehaviour
{
    static readonly Color BodyDark   = new Color(0.10f, 0.10f, 0.12f, 1f);
    static readonly Color BodyMid    = new Color(0.22f, 0.22f, 0.26f, 1f);
    static readonly Color SpikeColor = new Color(0.15f, 0.15f, 0.18f, 1f);
    static readonly Color EyeColor   = new Color(0.70f, 0.85f, 1.00f, 1f);

    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Stats")]
    [SerializeField] float maxHealth = 250f;

    [Header("Movement")]
    [SerializeField] float moveSpeed       = 15.0f;
    [SerializeField] float phase2MoveSpeed = 25.5f;
    [SerializeField] float stopDistance    = 1.8f;

    [Header("Attack — Ranges")]
    [SerializeField] float closeRangeThreshold = 4f;

    [Header("Attack — Close Slash")]
    [SerializeField] float telegraphDuration    = 0.40f;
    [SerializeField] float slashActiveDuration  = 0.30f;
    [SerializeField] float recoverDuration      = 0.30f;
    // Phase 1: single slash
    [SerializeField] float phase1SlashRadius    = 9f;
    [SerializeField] float phase1SlashAngle     = 220f;
    [SerializeField] float phase1AttackMin      = 2f;
    [SerializeField] float phase1AttackMax      = 3f;
    // Phase 2: rapid multi-slash combo
    [SerializeField] float phase2SlashRadius    = 30f;   // covers full arena
    [SerializeField] float phase2SlashAngle     = 360f;
    [SerializeField] float phase2AttackInterval = 0.5f;  // time between consecutive slashes
    [SerializeField] int   phase2SlashComboCount = 3;    // slashes per combo

    [Header("Attack — Rapid Fire (far range)")]
    [SerializeField] float      rapidFireCooldown = 0.10f;
    [SerializeField] GameObject bulletPrefab;

    [Header("Attack — Multi-Burst (idle, periodic)")]
    [SerializeField] float multiBurstInterval = 5f;
    [SerializeField] int   multiBurstCount    = 8;

    [Header("Attack — Berserk Rush")]
    [SerializeField] float berserkInterval      = 5f;
    [SerializeField] float berserkDuration      = 7.5f;
    [SerializeField] float berserkDirChangeTime = 0.30f;
    [SerializeField] int   berserkBulletCount   = 14;
    [SerializeField] float berserkSpeedMult     = 1.7f;
    [SerializeField] int   berserkContactDamage = 1;

    [Header("Attack — Phase 2 Sweep")]
    [SerializeField] float sweepWarningDuration = 2.0f;
    [SerializeField] int   sweepDamageToPlayer  = 1;
    [SerializeField] float phase2SweepInterval  = 20f;

    [Header("Damage")]
    [SerializeField] int   slashDamageToPlayer = 1;
    [SerializeField] float slashDamageOnBoss   = 5f;

    [Header("Auto-Start")]
    [SerializeField] float autoStartDelay = 5f;

    [Header("References")]
    [SerializeField] BossHealthBar healthBar;
    [SerializeField] BossIntroCam  bossIntroCam;

    [Header("Room Bounds")]
    [SerializeField] Vector2 roomCenter   = new Vector2(24f, 1f);
    [SerializeField] Vector2 roomHalfSize = new Vector2(11f, 11f);

    // ── State ─────────────────────────────────────────────────────────────────

    enum BossState { Dormant, Intro, Idle, CloseAttack, CloseAttackActive,
                     RapidFire, Berserk, Recover, Dead }
    BossState state = BossState.Dormant;

    float health;
    float attackTimer;
    float stateTimer;
    float sweepTimer;
    float berserkTimer;
    float multiBurstTimer;

    bool rapidFireRunning;
    bool berserkRunning;
    bool sweepScheduled;

    Transform      playerTransform;
    PlayerMovement playerMovement;
    Rigidbody2D    rb;

    Light2D          bodyGlow;
    SpriteRenderer[] allRenderers;
    Color[]          baseColors;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        health = maxHealth;
        rb     = GetComponent<Rigidbody2D>();
        BuildVisuals();
    }

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        playerMovement  = playerTransform?.GetComponent<PlayerMovement>();
        StartCoroutine(AutoStartAfterDelay());
    }

    IEnumerator AutoStartAfterDelay()
    {
        yield return new WaitForSeconds(autoStartDelay);
        if (state == BossState.Dormant)
            StartIntroSequence();
    }

    // ── Visuals ───────────────────────────────────────────────────────────────

    void BuildVisuals()
    {
        // Kill ALL pre-existing child Light2Ds (EyeLeft/EyeRight glow remnants)
        // This runs regardless of whether those GameObjects are active or not.
        foreach (var oldLight in GetComponentsInChildren<Light2D>(true))
            oldLight.enabled = false;

        // Also set the GameObjects themselves inactive by name
        foreach (Transform child in transform)
            if (child.name == "EyeLeft" || child.name == "EyeRight")
                child.gameObject.SetActive(false);

        Sprite   sq = CreateSquareSprite();
        Material m  = GetUnlitMat();

        var bodySR = GetComponent<SpriteRenderer>();
        if (bodySR != null)
        {
            bodySR.sprite       = sq;
            bodySR.color        = BodyDark;
            bodySR.material     = m;
            bodySR.sortingOrder = 5;
        }

        AddChildSprite("UmbraCore",     Vector3.zero,                        new Vector3(0.58f, 0.58f, 1f), BodyMid,    sq, m, 6);
        AddChildSprite("UmbraArmUp",    new Vector3( 0f,     0.85f,  0f),   new Vector3(0.26f, 0.95f, 1f), SpikeColor, sq, m, 5);
        AddChildSprite("UmbraArmDown",  new Vector3( 0f,    -0.85f,  0f),   new Vector3(0.26f, 0.95f, 1f), SpikeColor, sq, m, 5);
        AddChildSprite("UmbraArmLeft",  new Vector3(-0.85f,  0f,     0f),   new Vector3(0.95f, 0.26f, 1f), SpikeColor, sq, m, 5);
        AddChildSprite("UmbraArmRight", new Vector3( 0.85f,  0f,     0f),   new Vector3(0.95f, 0.26f, 1f), SpikeColor, sq, m, 5);
        AddChildSprite("UmbraDiagUL",   new Vector3(-0.55f,  0.55f,  0f),   new Vector3(0.36f, 0.36f, 1f), BodyMid,    sq, m, 5);
        AddChildSprite("UmbraDiagUR",   new Vector3( 0.55f,  0.55f,  0f),   new Vector3(0.36f, 0.36f, 1f), BodyMid,    sq, m, 5);
        AddChildSprite("UmbraDiagDL",   new Vector3(-0.55f, -0.55f,  0f),   new Vector3(0.36f, 0.36f, 1f), BodyMid,    sq, m, 5);
        AddChildSprite("UmbraDiagDR",   new Vector3( 0.55f, -0.55f,  0f),   new Vector3(0.36f, 0.36f, 1f), BodyMid,    sq, m, 5);
        AddChildSprite("UmbraEyeLeft",  new Vector3(-0.22f,  0.18f, -0.1f), new Vector3(0.44f, 0.13f, 1f), EyeColor,   sq, m, 7);
        AddChildSprite("UmbraEyeRight", new Vector3( 0.22f,  0.18f, -0.1f), new Vector3(0.44f, 0.13f, 1f), EyeColor,   sq, m, 7);

        var glowObj                    = new GameObject("UmbraGlow");
        glowObj.transform.SetParent(transform, false);
        bodyGlow                       = glowObj.AddComponent<Light2D>();
        bodyGlow.lightType             = Light2D.LightType.Point;
        bodyGlow.color                 = new Color(0.45f, 0.60f, 0.85f);
        bodyGlow.intensity             = 0.6f;
        bodyGlow.pointLightOuterRadius = 2.5f;
        bodyGlow.pointLightInnerRadius = 0.5f;
        bodyGlow.shadowsEnabled        = false;

        allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors   = new Color[allRenderers.Length];
        for (int i = 0; i < allRenderers.Length; i++)
            baseColors[i] = allRenderers[i].color;
    }

    void AddChildSprite(string n, Vector3 pos, Vector3 scale, Color color,
                        Sprite sprite, Material mat, int order)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        var sr          = go.AddComponent<SpriteRenderer>();
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
        if (_unlitMat != null) return _unlitMat;
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                  ?? Shader.Find("Sprites/Default");
        if (shader != null) _unlitMat = new Material(shader);
        return _unlitMat;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void BeginIntro()
    {
        if (state != BossState.Dormant) return;
        state = BossState.Intro;
    }

    public void StartIntroSequence()
    {
        if (state != BossState.Dormant && state != BossState.Intro) return;
        state = BossState.Idle;

        if (healthBar != null) { healthBar.Initialize(maxHealth); healthBar.Show(); }

        attackTimer     = Random.Range(phase1AttackMin, phase1AttackMax);
        berserkTimer    = berserkInterval;
        sweepTimer      = phase2SweepInterval;
        multiBurstTimer = multiBurstInterval;
    }

    public bool IsInBattle =>
        state != BossState.Dormant && state != BossState.Intro && state != BossState.Dead;

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (state == BossState.Dormant || state == BossState.Intro || state == BossState.Dead)
            return;

        stateTimer -= Time.deltaTime;

        // Phase 2 sweep — triggers only from Idle
        if (IsPhase2 && !sweepScheduled && state == BossState.Idle)
        {
            sweepTimer -= Time.deltaTime;
            if (sweepTimer <= 0f)
            {
                sweepTimer     = phase2SweepInterval;
                sweepScheduled = true;
                StartCoroutine(SweepAttack());
                return;
            }
        }

        // Multi-burst — fires in Idle/CloseAttack (not during berserk/rapid fire)
        if (state == BossState.Idle || state == BossState.CloseAttack ||
            state == BossState.CloseAttackActive)
        {
            multiBurstTimer -= Time.deltaTime;
            if (multiBurstTimer <= 0f)
            {
                multiBurstTimer = multiBurstInterval;
                FireMultiBurst();
            }
        }

        switch (state)
        {
            case BossState.Idle:              UpdateIdle();        break;
            case BossState.CloseAttack:       UpdateCloseAttack(); break;
            case BossState.CloseAttackActive: CheckSlashDone();    break;
            case BossState.Recover:           UpdateRecover();     break;
        }
    }

    void UpdateIdle()
    {
        if (playerTransform == null) return;

        // Berserk
        berserkTimer -= Time.deltaTime;
        if (berserkTimer <= 0f && !berserkRunning)
        {
            berserkTimer = berserkInterval;
            StartCoroutine(BerserkRush());
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        if (dist >= closeRangeThreshold)
        {
            if (!rapidFireRunning)
                StartCoroutine(DoRapidFire());
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            if (IsPhase2)
                StartCoroutine(Phase2SlashCombo());
            else
                EnterCloseAttack();
        }
    }

    void UpdateCloseAttack()
    {
        if (stateTimer <= 0f) EnterCloseAttackActive();
    }

    void CheckSlashDone()
    {
        if (stateTimer <= 0f) EnterRecover();
    }

    void UpdateRecover()
    {
        if (stateTimer <= 0f)
        {
            state       = BossState.Idle;
            attackTimer = IsPhase2
                ? phase2AttackInterval
                : Random.Range(phase1AttackMin, phase1AttackMax);
            RestoreBaseColors();
        }
    }

    // ── FixedUpdate — movement ────────────────────────────────────────────────

    void FixedUpdate()
    {
        if (rb == null) return;

        // Hard-clamp position every physics frame — prevents any state from escaping
        ClampToBounds();

        if (playerTransform == null) return;
        if (state == BossState.Berserk) return; // berserk moves itself

        if (state != BossState.Idle) { rb.linearVelocity = Vector2.zero; return; }

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= stopDistance || dist >= closeRangeThreshold)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float   speed = IsPhase2 ? phase2MoveSpeed : moveSpeed;
        Vector2 dir   = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * speed;
    }

    void ClampToBounds()
    {
        float margin = 1.2f;
        float minX = roomCenter.x - roomHalfSize.x + margin;
        float maxX = roomCenter.x + roomHalfSize.x - margin;
        float minY = roomCenter.y - roomHalfSize.y + margin;
        float maxY = roomCenter.y + roomHalfSize.y - margin;

        Vector2 pos = rb.position;
        bool clamped = false;
        if (pos.x < minX) { pos.x = minX; clamped = true; }
        if (pos.x > maxX) { pos.x = maxX; clamped = true; }
        if (pos.y < minY) { pos.y = minY; clamped = true; }
        if (pos.y > maxY) { pos.y = maxY; clamped = true; }

        if (clamped)
        {
            rb.MovePosition(pos);
            // Kill velocity component pointing out of bounds
            Vector2 vel = rb.linearVelocity;
            if (pos.x <= minX && vel.x < 0f) vel.x = 0f;
            if (pos.x >= maxX && vel.x > 0f) vel.x = 0f;
            if (pos.y <= minY && vel.y < 0f) vel.y = 0f;
            if (pos.y >= maxY && vel.y > 0f) vel.y = 0f;
            rb.linearVelocity = vel;
        }
    }

    // ── State Transitions ─────────────────────────────────────────────────────

    void EnterCloseAttack()
    {
        state      = BossState.CloseAttack;
        stateTimer = telegraphDuration;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        StartCoroutine(TelegraphPulse());
    }

    void EnterCloseAttackActive()
    {
        state      = BossState.CloseAttackActive;
        stateTimer = slashActiveDuration;
        FireBossSlash(phase1SlashRadius, phase1SlashAngle);
    }

    void EnterRecover()
    {
        state      = BossState.Recover;
        stateTimer = recoverDuration;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    // ── Phase 2 Slash Combo ───────────────────────────────────────────────────

    IEnumerator Phase2SlashCombo()
    {
        state = BossState.CloseAttack;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        for (int i = 0; i < phase2SlashComboCount; i++)
        {
            if (state == BossState.Dead) yield break;

            // Brief telegraph flash for each slash
            SetGlow(new Color(1f, 1f, 1f), 6f);
            yield return new WaitForSeconds(telegraphDuration * 0.6f);

            FireBossSlash(phase2SlashRadius, phase2SlashAngle);
            SetGlow(new Color(0.45f, 0.60f, 0.85f), 0.6f);

            yield return new WaitForSeconds(slashActiveDuration + phase2AttackInterval);
        }

        EnterRecover();
    }

    // ── Boss Slash ────────────────────────────────────────────────────────────

    void FireBossSlash(float radius, float angleDeg)
    {
        if (playerTransform == null) return;

        bool is360 = Mathf.Approximately(angleDeg, 360f);
        float aimAngle = 0f;
        if (!is360)
        {
            Vector2 aim = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            aimAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg - 90f;
        }

        var slashObj = new GameObject("UmbraSlashArc");
        slashObj.transform.position = transform.position;
        slashObj.transform.rotation = Quaternion.Euler(0f, 0f, aimAngle);

        float halfRad  = angleDeg * 0.5f * Mathf.Deg2Rad;
        int   segments = is360 ? 24 : 20;

        // Build polygon for collider
        var points = new Vector2[segments + 2];
        points[0] = Vector2.zero;
        for (int i = 0; i <= segments; i++)
        {
            float a = -halfRad + 2f * halfRad * i / segments;
            points[i + 1] = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * radius;
        }
        if (is360) points[segments + 1] = points[1]; // close circle

        var col = slashObj.AddComponent<PolygonCollider2D>();
        col.isTrigger = true;
        col.points    = points;

        BuildBossSlashMesh(slashObj, halfRad, segments, radius, is360);

        var light                   = slashObj.AddComponent<Light2D>();
        light.lightType             = Light2D.LightType.Point;
        light.color                 = is360 ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.5f, 0.65f, 0.9f);
        light.intensity             = is360 ? 3.5f : 1.5f;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * 0.1f;
        light.shadowsEnabled        = false;

        slashObj.AddComponent<BossSlashHitbox>().Init(slashDamageToPlayer);

        var fader    = slashObj.AddComponent<SlashFader>();
        fader.duration = slashActiveDuration;
    }

    void BuildBossSlashMesh(GameObject parent, float halfRad, int segs, float radius, bool is360)
    {
        var meshObj = new GameObject("BossSlashMesh");
        meshObj.transform.SetParent(parent.transform, false);
        var mf     = meshObj.AddComponent<MeshFilter>();
        var mr     = meshObj.AddComponent<MeshRenderer>();
        var shader = Shader.Find("Sprites/Default");
        if (shader != null) mr.material = new Material(shader);
        mr.sortingOrder = 10;

        int   vc    = segs + 2;
        var   verts = new Vector3[vc];
        var   cols  = new Color[vc];
        verts[0] = Vector3.zero;
        // 360° sweep is greyed-out; directional slash is blue-white
        cols[0]  = is360
            ? new Color(0.6f, 0.6f, 0.6f, 0.25f)
            : new Color(0.45f, 0.60f, 0.85f, 0.28f);

        for (int i = 0; i <= segs; i++)
        {
            float a = -halfRad + 2f * halfRad * i / segs;
            verts[i + 1] = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * radius;
            cols[i + 1]  = is360
                ? new Color(0.75f, 0.75f, 0.75f, 0.55f)
                : new Color(0.80f, 0.90f, 1.00f, 0.62f);
        }

        var tris = new int[segs * 3];
        for (int i = 0; i < segs; i++)
        { tris[i * 3] = 0; tris[i * 3 + 1] = i + 1; tris[i * 3 + 2] = i + 2; }

        var mesh       = new Mesh();
        mesh.vertices  = verts;
        mesh.triangles = tris;
        mesh.colors    = cols;
        mesh.RecalculateNormals();
        mf.mesh = mesh;

        // Edge line only for directional slashes
        if (!is360)
        {
            var edgeObj = new GameObject("BossSlashEdge");
            edgeObj.transform.SetParent(parent.transform, false);
            var lr           = edgeObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            if (shader != null) lr.material = new Material(shader);
            lr.startColor    = new Color(0.85f, 0.92f, 1f, 0.9f);
            lr.endColor      = new Color(0.85f, 0.92f, 1f, 0.9f);
            lr.startWidth    = 0.10f;
            lr.endWidth      = 0.10f;
            lr.sortingOrder  = 11;
            int lp           = segs + 3;
            lr.positionCount = lp;
            lr.SetPosition(0, Vector3.zero);
            for (int i = 0; i <= segs; i++)
            {
                float a = -halfRad + 2f * halfRad * i / segs;
                lr.SetPosition(i + 1, new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * radius);
            }
            lr.SetPosition(lp - 1, Vector3.zero);
        }
    }

    // ── Rapid Fire ────────────────────────────────────────────────────────────

    IEnumerator DoRapidFire()
    {
        rapidFireRunning = true;
        state            = BossState.RapidFire;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        SetGlow(new Color(0.3f, 0.5f, 1.0f), 2.0f);

        while (state == BossState.RapidFire)
        {
            if (playerTransform == null) break;
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist < closeRangeThreshold) break;
            FireBulletAtPlayer();
            yield return new WaitForSeconds(rapidFireCooldown);
        }

        rapidFireRunning = false;
        SetGlow(new Color(0.45f, 0.60f, 0.85f), 0.6f);
        if (state == BossState.RapidFire)
        {
            state       = BossState.Idle;
            attackTimer = Random.Range(phase1AttackMin, phase1AttackMax);
        }
    }

    void FireBulletAtPlayer()
    {
        if (bulletPrefab == null || playerTransform == null) return;
        float angle = Mathf.Atan2(
            playerTransform.position.y - transform.position.y,
            playerTransform.position.x - transform.position.x) * Mathf.Rad2Deg;
        SpawnBullet(angle);
    }

    // ── Multi-Burst ───────────────────────────────────────────────────────────

    void FireMultiBurst()
    {
        if (bulletPrefab == null) return;
        // Aim first bullet toward player, then spread evenly around
        float baseAngle = 0f;
        if (playerTransform != null)
            baseAngle = Mathf.Atan2(
                playerTransform.position.y - transform.position.y,
                playerTransform.position.x - transform.position.x) * Mathf.Rad2Deg;

        float step = 360f / multiBurstCount;
        for (int i = 0; i < multiBurstCount; i++)
            SpawnBullet(baseAngle + step * i);
    }

    // ── Berserk Rush ──────────────────────────────────────────────────────────

    IEnumerator BerserkRush()
    {
        berserkRunning = true;
        state          = BossState.Berserk;

        SetGlow(new Color(1f, 0.15f, 0.15f), 5f);

        // Red flash on body
        if (allRenderers != null)
            for (int i = 0; i < allRenderers.Length; i++)
                if (allRenderers[i] != null)
                    allRenderers[i].color = Color.Lerp(baseColors[i], new Color(1f, 0.2f, 0.2f), 0.65f);

        float   elapsed    = 0f;
        float   dirTimer   = 0f;
        float   speed      = (IsPhase2 ? phase2MoveSpeed : moveSpeed) * berserkSpeedMult;
        Vector2 dir        = Vector2.up;

        while (elapsed < berserkDuration && state == BossState.Berserk)
        {
            elapsed  += Time.deltaTime;
            dirTimer -= Time.deltaTime;

            if (dirTimer <= 0f && playerTransform != null)
            {
                // Chase player with a random zig-zag offset
                Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                float   baseAng  = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
                float   offset   = Random.Range(30f, 70f) * (Random.value > 0.5f ? 1f : -1f);
                float   rad      = (baseAng + offset) * Mathf.Deg2Rad;
                dir              = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                dirTimer         = berserkDirChangeTime;

                FireOmniBurst(); // burst on every zig turn
            }

            // Bounce off room walls based on CURRENT position (safe at high speed)
            Vector2 cur    = rb != null ? rb.position : (Vector2)transform.position;
            float   margin = 1.5f;
            bool    bounced = false;
            if (cur.x < roomCenter.x - roomHalfSize.x + margin) { dir.x =  Mathf.Abs(dir.x); bounced = true; }
            if (cur.x > roomCenter.x + roomHalfSize.x - margin) { dir.x = -Mathf.Abs(dir.x); bounced = true; }
            if (cur.y < roomCenter.y - roomHalfSize.y + margin) { dir.y =  Mathf.Abs(dir.y); bounced = true; }
            if (cur.y > roomCenter.y + roomHalfSize.y - margin) { dir.y = -Mathf.Abs(dir.y); bounced = true; }
            if (bounced) { dir = dir.normalized; FireOmniBurst(); }

            if (rb != null)
                rb.linearVelocity = dir * speed;

            yield return null;
        }

        berserkRunning = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        RestoreBaseColors();
        SetGlow(new Color(0.45f, 0.60f, 0.85f), 0.6f);

        if (state == BossState.Berserk)
        {
            state       = BossState.Idle;
            attackTimer = Random.Range(phase1AttackMin, phase1AttackMax);
        }
    }

    void FireOmniBurst()
    {
        if (bulletPrefab == null) return;
        float step = 360f / berserkBulletCount;
        for (int i = 0; i < berserkBulletCount; i++)
            SpawnBullet(step * i);
    }

    void SpawnBullet(float angleDeg)
    {
        if (bulletPrefab == null) return;
        var bullet = Instantiate(bulletPrefab, transform.position,
                                 Quaternion.Euler(0f, 0f, angleDeg - 90f));
        // Tag as EnemyBullet so Bullet.Start() applies correct visuals,
        // deals damage to player on hit, and auto-destroys after 3 s.
        bullet.tag = "EnemyBullet";
    }

    // ── Phase 2 Sweep ─────────────────────────────────────────────────────────

    IEnumerator SweepAttack()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Build grey world overlay + screen warning UI
        var overlay = BuildSweepOverlay();
        var warnUI  = BuildSweepWarningUI();

        SetGlow(new Color(0.85f, 0.85f, 0.85f), 5f);

        float elapsed = 0f;
        while (elapsed < sweepWarningDuration)
        {
            elapsed += Time.deltaTime;
            float t     = elapsed / sweepWarningDuration;
            float pulse = Mathf.PingPong(elapsed * 3.5f, 1f);

            // Greyscale overlay pulses darker
            if (overlay != null)
            {
                var sr  = overlay.GetComponent<SpriteRenderer>();
                sr.color = new Color(0.65f, 0.65f, 0.65f, Mathf.Lerp(0.08f, 0.40f, pulse));
            }

            // Warning UI fades/pulses
            if (warnUI != null)
            {
                float alpha = Mathf.Lerp(0.4f, 1f, pulse);
                UpdateSweepWarningAlpha(warnUI, alpha);
            }

            yield return null;
        }

        // STRIKE — bright grey flash
        if (overlay != null)
            overlay.GetComponent<SpriteRenderer>().color = new Color(0.85f, 0.85f, 0.85f, 0.70f);

        // Damage player unless dashing
        if (playerTransform != null)
        {
            bool safe = playerMovement != null && playerMovement.IsDashing;
            if (!safe)
                playerTransform.GetComponent<PlayerHealth>()?.TakeDamage(sweepDamageToPlayer);
        }

        yield return new WaitForSeconds(0.15f);

        // Destroy warning UI
        if (warnUI != null) Destroy(warnUI);

        // Fade out overlay
        if (overlay != null)
        {
            var sr = overlay.GetComponent<SpriteRenderer>();
            float t = 0f;
            while (t < 0.45f && overlay != null)
            {
                t += Time.deltaTime;
                var c = sr.color;
                c.a = Mathf.Lerp(0.70f, 0f, t / 0.45f);
                sr.color = c;
                yield return null;
            }
            Destroy(overlay);
        }

        SetGlow(new Color(0.45f, 0.60f, 0.85f), 0.6f);
        sweepScheduled = false;
    }

    GameObject BuildSweepOverlay()
    {
        var obj = new GameObject("UmbraSweepOverlay");
        obj.transform.position = new Vector3(roomCenter.x, roomCenter.y, -0.5f);
        var sr       = obj.AddComponent<SpriteRenderer>();
        sr.sprite       = CreateSquareSprite();
        sr.color        = new Color(0.6f, 0.6f, 0.6f, 0f);
        sr.sortingOrder = 20;
        float w = roomHalfSize.x * 2.4f;
        float h = roomHalfSize.y * 2.4f;
        obj.transform.localScale = new Vector3(w, h, 1f);
        return obj;
    }

    // Screen-space warning canvas (no TMP needed)
    GameObject BuildSweepWarningUI()
    {
        var canvasObj = new GameObject("SweepWarnCanvas");
        var canvas    = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        var scaler                 = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // Edge-flash panels (top, bottom, left, right)
        CreateEdgePanel(canvasObj, "Top",    new Vector2(0.5f, 1f), new Vector2(1f, 0f),   new Vector2(0f, -8f),  new Vector2(1920f, 18f));
        CreateEdgePanel(canvasObj, "Bottom", new Vector2(0.5f, 0f), new Vector2(1f, 1f),   new Vector2(0f,  8f),  new Vector2(1920f, 18f));
        CreateEdgePanel(canvasObj, "Left",   new Vector2(0f,  0.5f), new Vector2(1f, 0.5f), new Vector2( 8f, 0f), new Vector2(18f, 1080f));
        CreateEdgePanel(canvasObj, "Right",  new Vector2(1f,  0.5f), new Vector2(0f, 0.5f), new Vector2(-8f, 0f), new Vector2(18f, 1080f));

        // Centre warning text
        var textObj = new GameObject("WarnText");
        textObj.transform.SetParent(canvasObj.transform, false);
        var textRT          = textObj.AddComponent<RectTransform>();
        textRT.anchorMin    = new Vector2(0.5f, 0.75f);
        textRT.anchorMax    = new Vector2(0.5f, 0.75f);
        textRT.sizeDelta    = new Vector2(700f, 110f);
        textRT.anchoredPosition = Vector2.zero;
        var text            = textObj.AddComponent<Text>();
        text.text           = "!! INCOMING - DASH TO SURVIVE !!";
        text.fontSize       = 52;
        text.fontStyle      = FontStyle.Bold;
        text.alignment      = TextAnchor.MiddleCenter;
        text.color          = new Color(0.9f, 0.9f, 0.9f, 1f);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null) text.font = font;

        return canvasObj;
    }

    void CreateEdgePanel(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax,
                         Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var obj  = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        var rt   = obj.AddComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMin;
        rt.pivot            = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
        var img  = obj.AddComponent<Image>();
        img.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        img.raycastTarget = false;
    }

    void UpdateSweepWarningAlpha(GameObject ui, float alpha)
    {
        foreach (var img in ui.GetComponentsInChildren<Image>(true))
            img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
        foreach (var txt in ui.GetComponentsInChildren<Text>(true))
            txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, alpha);
    }

    // ── Contact Damage (berserk) ──────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        if (state == BossState.Dead) return;

        if (state == BossState.Berserk && other.CompareTag("Player"))
        {
            other.GetComponent<PlayerHealth>()?.TakeDamage(berserkContactDamage);
            return;
        }

        if (other.CompareTag("LightSource") && other.GetComponent<SlashBulletDeflector>() != null)
            ApplyDamage(slashDamageOnBoss);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Continuous contact damage during berserk (iFrames on PlayerHealth prevent spam)
        if (state == BossState.Dead || state != BossState.Berserk) return;
        if (other.CompareTag("Player"))
            other.GetComponent<PlayerHealth>()?.TakeDamage(berserkContactDamage);
    }

    void ApplyDamage(float amount)
    {
        health -= amount;
        health  = Mathf.Max(health, 0f);
        if (healthBar != null) healthBar.SetHealth(health);
        DamageNumber.Spawn(Mathf.CeilToInt(amount), transform.position);
        StartCoroutine(HitFlash());
        if (health <= 0f) { StopAllCoroutines(); StartCoroutine(DeathSequence()); }
    }

    bool IsPhase2 => health <= maxHealth * 0.40f;

    // ── Visuals helpers ───────────────────────────────────────────────────────

    void SetGlow(Color c, float intensity)
    {
        if (bodyGlow == null) return;
        bodyGlow.color     = c;
        bodyGlow.intensity = intensity;
    }

    void RestoreBaseColors()
    {
        if (allRenderers == null) return;
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] == null) continue;
            allRenderers[i].color = baseColors[i];
        }
    }

    IEnumerator TelegraphPulse()
    {
        float elapsed = 0f;
        while (elapsed < telegraphDuration && state == BossState.CloseAttack)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 5f, 1f);
            if (allRenderers != null)
                for (int i = 0; i < allRenderers.Length; i++)
                    if (allRenderers[i] != null)
                        allRenderers[i].color = Color.Lerp(
                            baseColors[i],
                            new Color(0.9f, 0.9f, 1.0f, baseColors[i].a),
                            t * 0.9f);
            SetGlow(Color.Lerp(new Color(0.45f, 0.60f, 0.85f), Color.white, t),
                    Mathf.Lerp(0.6f, 6f, t));
            yield return null;
        }
    }

    IEnumerator HitFlash()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        Color orig = sr.color;
        sr.color = Color.white;
        yield return new WaitForSeconds(0.07f);
        if (sr != null) sr.color = orig;
    }

    // ── Death ─────────────────────────────────────────────────────────────────

    IEnumerator DeathSequence()
    {
        if (state == BossState.Dead) yield break;
        state = BossState.Dead;

        if (healthBar != null) healthBar.Hide();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        var   renderers = GetComponentsInChildren<SpriteRenderer>(true);
        var   startCols = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) startCols[i] = renderers[i].color;

        float elapsed = 0f;
        const float dur = 1.8f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                var c = startCols[i]; c.a = Mathf.Lerp(1f, 0f, t); renderers[i].color = c;
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

// ── BossSlashHitbox ───────────────────────────────────────────────────────────

public class BossSlashHitbox : MonoBehaviour
{
    int  damage;
    bool hasHit;

    public void Init(int dmg) { damage = dmg; }

    void OnTriggerEnter2D(Collider2D other) => Evaluate(other);
    void OnTriggerStay2D(Collider2D other)  => Evaluate(other);

    void Evaluate(Collider2D other)
    {
        if (hasHit) return;
        if (!other.CompareTag("Player")) return;
        hasHit = true;
        other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
    }
}
