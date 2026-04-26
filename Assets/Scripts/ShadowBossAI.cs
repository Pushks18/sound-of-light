using UnityEngine;
using UnityEngine.Rendering.Universal;
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
    [SerializeField] float phase1AttackMin      = 3f;
    [SerializeField] float phase1AttackMax      = 5f;
    // Phase 2: single slash (wider arc, shorter cooldown than phase 1)
    [SerializeField] float phase2SlashRadius    = 30f;
    [SerializeField] float phase2SlashAngle     = 280f;
    [SerializeField] float phase2AttackInterval = 4f;

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
                     RapidFire, Berserk, Recover, Stunned, Dead }
    BossState state = BossState.Dormant;

    float health;
    float attackTimer;
    float stateTimer;
    float berserkTimer;
    float multiBurstTimer;

    bool  rapidFireRunning;
    bool  berserkRunning;
    bool  berserkPlayerHit;
    float attackLockTimer;
    BossSlashHitbox lastSlashHitbox;
    float           lastSlashRadius;

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
        multiBurstTimer = multiBurstInterval;
    }

    public bool IsInBattle =>
        state != BossState.Dormant && state != BossState.Intro && state != BossState.Dead;

    public IEnumerator SpecialStun(float duration)
    {
        state = BossState.Stunned;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        RestoreBaseColors();
        SetGlow(new Color(0.9f, 0.9f, 0.2f), 3f);

        yield return new WaitForSeconds(duration);

        SetGlow(new Color(0.45f, 0.60f, 0.85f), 0.6f);
        state           = BossState.Idle;
        attackTimer     = Random.Range(phase1AttackMin, phase1AttackMax);
        berserkTimer    = berserkInterval;
        multiBurstTimer = multiBurstInterval;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (state == BossState.Dormant || state == BossState.Intro || state == BossState.Dead)
            return;

        if (Input.GetKeyDown(KeyCode.L) && state != BossState.Stunned)
        {
            StopAllCoroutines();
            rapidFireRunning = false;
            berserkRunning   = false;
            StartCoroutine(SpecialStun(3f));
        }

        if (state == BossState.Stunned)
            return;

        stateTimer -= Time.deltaTime;

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

        if (attackLockTimer > 0f) attackLockTimer -= Time.deltaTime;

        // Berserk (not gated by attackLockTimer)
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
            if (!rapidFireRunning && attackLockTimer <= 0f)
                StartCoroutine(DoRapidFire());
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f && attackLockTimer <= 0f)
            EnterCloseAttack();
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
            Vector2 toCenter = roomCenter - (Vector2)transform.position;
            rb.linearVelocity = toCenter.magnitude > 3f ? toCenter.normalized * 1.5f : Vector2.zero;
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
        stateTimer = slashActiveDuration + 0.15f; // freeze time + active time
        StartCoroutine(SlashWithFreeze());
    }

    IEnumerator SlashWithFreeze()
    {
        // Hard white peak — clear "it's about to slash" signal
        SetGlow(Color.white, 10f);
        if (allRenderers != null)
            for (int i = 0; i < allRenderers.Length; i++)
                if (allRenderers[i] != null)
                    allRenderers[i].color = Color.white;
        yield return new WaitForSeconds(0.15f);
        if (state != BossState.CloseAttackActive) yield break;
        float radius = IsPhase2 ? phase2SlashRadius : phase1SlashRadius;
        float angle  = IsPhase2 ? phase2SlashAngle  : phase1SlashAngle;
        FireBossSlash(radius, angle);
    }

    void EnterRecover()
    {
        if (lastSlashHitbox != null && !lastSlashHitbox.DidHit && playerTransform != null)
        {
            float d = Vector2.Distance(transform.position, playerTransform.position);
            if (d < lastSlashRadius + 2f)
                StartCoroutine(NearMissFlicker());
        }
        lastSlashHitbox = null;

        state      = BossState.Recover;
        stateTimer = recoverDuration;
        if (rb != null) rb.linearVelocity = Vector2.zero;
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

        lastSlashHitbox = slashObj.AddComponent<BossSlashHitbox>();
        lastSlashHitbox.Init(slashDamageToPlayer);
        lastSlashRadius = radius;

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

        // Pre-fire charge signal
        SetGlow(new Color(0.1f, 0.3f, 1.0f), 5.0f);
        yield return new WaitForSeconds(0.2f);
        if (state != BossState.RapidFire) { rapidFireRunning = false; yield break; }
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
        float angle  = Mathf.Atan2(
            playerTransform.position.y - transform.position.y,
            playerTransform.position.x - transform.position.x) * Mathf.Rad2Deg;
        float spread = IsPhase2 ? Random.Range(-5f, 5f) : 0f;
        SpawnBullet(angle + spread);
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
        {
            float spiralOff = IsPhase2 ? i * (step * 0.15f) : 0f;
            SpawnBullet(baseAngle + step * i + spiralOff);
        }

        attackLockTimer = 0.4f;
    }

    // ── Berserk Rush ──────────────────────────────────────────────────────────

    IEnumerator BerserkRush()
    {
        berserkRunning   = true;
        berserkPlayerHit = false;

        // Anticipation — red ramp before entering berserk
        SetGlow(new Color(0.9f, 0.05f, 0.05f), 7f);
        yield return new WaitForSeconds(0.25f);
        if (state == BossState.Dead) { berserkRunning = false; yield break; }

        state = BossState.Berserk;
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

        if (state == BossState.Berserk)
        {
            // Vulnerability window — boss is open to punishment
            state = BossState.Recover;
            SetGlow(new Color(0.2f, 0.2f, 0.35f), 0.25f);
            yield return new WaitForSeconds(1.0f);

            if (!berserkPlayerHit)
                StartCoroutine(GlowJitter()); // player dodged whole berserk

            SetGlow(new Color(0.45f, 0.60f, 0.85f), 0.6f);
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

    // ── Contact Damage (berserk) ──────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        if (state == BossState.Dead) return;

        if (state == BossState.Berserk && other.CompareTag("Player"))
        {
            berserkPlayerHit = true;
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
        {
            berserkPlayerHit = true;
            other.GetComponent<PlayerHealth>()?.TakeDamage(berserkContactDamage);
        }
    }

    void ApplyDamage(float amount)
    {
        health -= amount;
        health  = Mathf.Max(health, 0f);
        if (healthBar != null) healthBar.SetHealth(health);
        DamageNumber.Spawn(Mathf.CeilToInt(amount), transform.position);
        StartCoroutine(HitFlash());
        if (state == BossState.Recover) StartCoroutine(RecoveryStagger());
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
                            new Color(1f, 1f, 1f, baseColors[i].a),
                            t);
            SetGlow(Color.Lerp(new Color(0.45f, 0.60f, 0.85f), Color.white, t),
                    Mathf.Lerp(0.6f, 6f, t));
            yield return null;
        }
    }

    IEnumerator NearMissFlicker()
    {
        SetGlow(new Color(0.8f, 0.8f, 1f), 5f);
        yield return new WaitForSeconds(0.1f);
        SetGlow(new Color(0.45f, 0.60f, 0.85f), 0.6f);
    }

    IEnumerator GlowJitter()
    {
        for (int i = 0; i < 4; i++)
        {
            SetGlow(new Color(0.55f, 0.25f, 0.9f), 4f);
            yield return new WaitForSeconds(0.07f);
            SetGlow(new Color(0.45f, 0.60f, 0.85f), 0.6f);
            yield return new WaitForSeconds(0.07f);
        }
    }

    IEnumerator RecoveryStagger()
    {
        SetGlow(new Color(0.15f, 0.15f, 0.2f), 1.0f);
        yield return new WaitForSeconds(0.12f);
        SetGlow(new Color(0.45f, 0.60f, 0.85f), 0.6f);
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

    public bool DidHit => hasHit;
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
