using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Boss AI for Umbra — IsshinBossScene.
///
/// COMBAT:
///   Close  → large slash, phase1: 2-3 s interval / phase2: combo of slashes with dodge windows.
///   Far    → rapid bullets (0.1 s cooldown).
///   Idle   → periodic multi-directional bullet burst every ~5 s.
///   Berserk→ every 5 s: chases player at high speed, zig-zags, contact dmg,
///            omni-bursts on each zig turn. Lasts ~4 s.
///            Flash (L key) during berserk → boss stunned for 1.3 s.
///   Phase2 → periodic full-arena SWEEP: grey overlay + screen warning,
///            player must DASH to survive.
///            Entering phase 2: ground shake + "!!!" + camera zoom.
///            Dash during a phase-2 slash → boss takes 10 bonus damage.
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
    [SerializeField] float telegraphDuration    = 1.00f;  // increased subtly for dodge room
    [SerializeField] float slashActiveDuration  = 0.50f;
    [SerializeField] float recoverDuration      = 0.50f;
    // Phase 1: single slash
    [SerializeField] float phase1SlashRadius    = 9f;
    [SerializeField] float phase1SlashAngle     = 220f;
    [SerializeField] float phase1AttackMin      = 2f;
    [SerializeField] float phase1AttackMax      = 3f;
    // Phase 2: rapid multi-slash combo — wider gaps so player can dodge
    [SerializeField] float phase2SlashRadius    = 30f;
    [SerializeField] float phase2SlashAngle     = 360f;
    [SerializeField] float phase2AttackInterval = 1.3f;   // time between consecutive slashes (was 0.5 — now gives dodge room)
    [SerializeField] float phase2ComboCD        = 3.5f;   // cooldown after full combo before next one
    [SerializeField] float phase2TelegraphTime  = 0.70f;  // per-slash telegraph in phase 2 (longer = more flash warning)
    [SerializeField] int   phase2SlashComboCount = 3;

    [Header("Attack — Rapid Fire (far range)")]
    [SerializeField] float      rapidFireCooldown = 0.10f;
    [SerializeField] GameObject bulletPrefab;

    [Header("Attack — Multi-Burst (idle, periodic)")]
    [SerializeField] float multiBurstInterval = 5f;
    [SerializeField] int   multiBurstCount    = 8;

    [Header("Attack — Berserk Rush")]
    [SerializeField] float berserkInterval          = 3f;
    [SerializeField] float berserkDuration          = 3.0f;   // short berserk window; flash ends it early
    [SerializeField] float berserkDirChangeTime     = 0.30f;
    [SerializeField] int   berserkBulletCount       = 14;
    [SerializeField] float berserkSpeedMult         = 1.7f;
    [SerializeField] int   berserkContactDamage     = 1;
    [SerializeField] float berserkFlashStunDuration = 6f;   // stun when player flashes during berserk

    [Header("Attack — Phase 2 Sweep")]
    [SerializeField] float sweepWarningDuration = 3.0f;
    [SerializeField] int   sweepDamageToPlayer  = 3;
    [SerializeField] float phase2SweepInterval  = 20f;

    [Header("Damage")]
    [SerializeField] int   slashDamageToPlayer  = 1;
    [SerializeField] float slashDamageOnBoss    = 5f;
    [SerializeField] float dashDodgeBonusDamage = 10f;  // bonus damage boss takes when player dashes a phase-2 slash

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
    bool berserkStunned;
    bool sweepScheduled;
    bool phase2EffectsPlayed;
    bool specialStunned;      // true while player boomerang special is active

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
        foreach (var oldLight in GetComponentsInChildren<Light2D>(true))
            oldLight.enabled = false;

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

        // Boomerang special — boss completely frozen while it's active
        if (specialStunned) return;

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
            // Phase 2 has a longer cooldown between combos so player has room to attack
            attackTimer = IsPhase2
                ? phase2ComboCD
                : Random.Range(phase1AttackMin, phase1AttackMax);
            RestoreBaseColors();
        }
    }

    // ── FixedUpdate — movement ────────────────────────────────────────────────

    void FixedUpdate()
    {
        if (rb == null) return;

        ClampToBounds();

        // Freeze completely during player boomerang special
        if (specialStunned) { rb.linearVelocity = Vector2.zero; return; }

        if (playerTransform == null) return;
        if (state == BossState.Berserk) return;

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

            // Longer telegraph flash — gives player time to spot the warning and dash
            float telegraphT = 0f;
            while (telegraphT < phase2TelegraphTime)
            {
                telegraphT += Time.deltaTime;
                float pulse = Mathf.PingPong(telegraphT * 6f, 1f);
                SetGlow(Color.Lerp(new Color(0.45f, 0.60f, 0.85f), Color.white, pulse),
                        Mathf.Lerp(0.6f, 8f, pulse));

                // Flash body white during warning
                if (allRenderers != null)
                    for (int k = 0; k < allRenderers.Length; k++)
                        if (allRenderers[k] != null)
                            allRenderers[k].color = Color.Lerp(
                                baseColors[k],
                                new Color(0.95f, 0.95f, 1.0f, baseColors[k].a),
                                pulse * 0.85f);
                yield return null;
            }

            RestoreBaseColors();
            FireBossSlash(phase2SlashRadius, phase2SlashAngle);
            SetGlow(new Color(0.45f, 0.60f, 0.85f), 0.6f);

            // Check if player dashed during the slash active window → bonus damage
            yield return StartCoroutine(CheckDashDodgeDamage(slashActiveDuration));

            // Pause between slashes — room for player to counter-attack
            yield return new WaitForSeconds(phase2AttackInterval);
        }

        EnterRecover();
    }

    // If player is dashing while the full-arena slash is active, they dodged it → boss takes bonus damage
    IEnumerator CheckDashDodgeDamage(float windowDuration)
    {
        bool dodgeRegistered = false;
        float elapsed = 0f;
        while (elapsed < windowDuration)
        {
            elapsed += Time.deltaTime;
            if (!dodgeRegistered && playerMovement != null && playerMovement.IsDashing
                && playerTransform != null)
            {
                float dist = Vector2.Distance(transform.position, playerTransform.position);
                if (dist <= phase2SlashRadius)
                {
                    dodgeRegistered = true;
                    ApplyDamage(dashDodgeBonusDamage);
                    StartCoroutine(DashDodgeFeedback());
                }
            }
            yield return null;
        }
    }

    // Brief visual feedback when player successfully dash-dodges a slash
    IEnumerator DashDodgeFeedback()
    {
        SetGlow(new Color(1f, 0.4f, 0.1f), 7f);
        CameraShake.Instance?.Shake(0.12f, 0.18f);
        yield return new WaitForSeconds(0.25f);
        SetGlow(new Color(0.45f, 0.60f, 0.85f), 0.6f);
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

        var points = new Vector2[segments + 2];
        points[0] = Vector2.zero;
        for (int i = 0; i <= segments; i++)
        {
            float a = -halfRad + 2f * halfRad * i / segments;
            points[i + 1] = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * radius;
        }
        if (is360) points[segments + 1] = points[1];

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
        berserkStunned = false;
        state          = BossState.Berserk;

        SetGlow(new Color(1f, 0.15f, 0.15f), 5f);

        if (allRenderers != null)
            for (int i = 0; i < allRenderers.Length; i++)
                if (allRenderers[i] != null)
                    allRenderers[i].color = Color.Lerp(baseColors[i], new Color(1f, 0.2f, 0.2f), 0.65f);

        // Brief camera zoom-out on berserk start to show the arena
        StartCoroutine(BerserkCameraZoom());

        float   elapsed    = 0f;
        float   dirTimer   = 0f;
        float   speed      = (IsPhase2 ? phase2MoveSpeed : moveSpeed) * berserkSpeedMult;
        Vector2 dir        = Vector2.up;

        while (elapsed < berserkDuration && state == BossState.Berserk)
        {
            elapsed  += Time.deltaTime;
            dirTimer -= Time.deltaTime;

            // Flash stun check — if stunned, pause movement
            if (berserkStunned)
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                yield return null;
                continue;
            }

            if (dirTimer <= 0f && playerTransform != null)
            {
                Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                float   baseAng  = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
                float   offset   = Random.Range(30f, 70f) * (Random.value > 0.5f ? 1f : -1f);
                float   rad      = (baseAng + offset) * Mathf.Deg2Rad;
                dir              = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                dirTimer         = berserkDirChangeTime;

                FireOmniBurst();
            }

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
        berserkStunned = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        RestoreBaseColors();
        SetGlow(new Color(0.45f, 0.60f, 0.85f), 0.6f);

        if (state == BossState.Berserk)
        {
            state       = BossState.Idle;
            attackTimer = Random.Range(phase1AttackMin, phase1AttackMax);
        }
    }

    // Flash during berserk → freeze boss for stun duration, then END berserk entirely
    IEnumerator BerserkFlashStun()
    {
        berserkStunned = true;

        // Boss turns icy cyan — visually frozen
        SetGlow(new Color(0.4f, 1f, 1f), 10f);
        if (allRenderers != null)
            for (int i = 0; i < allRenderers.Length; i++)
                if (allRenderers[i] != null)
                    allRenderers[i].color = Color.Lerp(baseColors[i], new Color(0.55f, 0.95f, 1f), 0.9f);

        CameraShake.Instance?.Shake(0.22f, 0.30f);

        // Frozen duration — boss can't move
        yield return new WaitForSeconds(berserkFlashStunDuration);

        // Berserk ends: force state back to Idle
        berserkStunned = false;
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

    // Public: lets PlayerBoomerangDash deal direct damage to this boss
    public void TakeBoomerangDamage(float amount) => ApplyDamage(amount);

    // Super dash hit — damage + launch the boss far in the given direction
    public IEnumerator TakeSuperDashHit(float damage, Vector2 launchDir, float launchDist)
    {
        specialStunned = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        ApplyDamage(damage);

        // Visual: brief white-hot flash then dark stagger
        SetGlow(new Color(1f, 0.85f, 0.3f), 12f);
        if (allRenderers != null)
            for (int i = 0; i < allRenderers.Length; i++)
                if (allRenderers[i] != null)
                    allRenderers[i].color = Color.white;

        yield return new WaitForSeconds(0.08f);

        // Fly to launch target
        Vector2 from   = transform.position;
        Vector2 target = ClampToRoomPublic((Vector2)transform.position + launchDir * launchDist);
        float   dur    = 0.38f;
        float   t      = 0f;

        SetGlow(new Color(1f, 0.5f, 0.1f), 6f);
        RestoreBaseColors();

        while (t < dur)
        {
            t += Time.deltaTime;
            float ease = Mathf.SmoothStep(0f, 1f, t / dur);
            if (rb != null) rb.MovePosition(Vector2.Lerp(from, target, ease));
            else            transform.position = Vector2.Lerp(from, target, ease);
            yield return null;
        }
        if (rb != null) rb.MovePosition(target);

        // Slam into far wall — shake + stagger pause
        CameraShake.Instance?.Shake(0.35f, 0.45f);
        yield return new WaitForSeconds(0.55f);

        specialStunned = false;
        SetGlow(new Color(0.45f, 0.60f, 0.85f), 0.6f);
    }

    // Expose ClampToBounds for external callers
    public Vector2 ClampToRoomPublic(Vector2 pos)
    {
        float margin = 1.2f;
        float minX = roomCenter.x - roomHalfSize.x + margin;
        float maxX = roomCenter.x + roomHalfSize.x - margin;
        float minY = roomCenter.y - roomHalfSize.y + margin;
        float maxY = roomCenter.y + roomHalfSize.y - margin;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        return pos;
    }

    // Freeze the boss for the boomerang special duration
    public IEnumerator SpecialStun(float duration)
    {
        specialStunned = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Freeze visual: slight dark pulse
        SetGlow(new Color(0.2f, 0.2f, 0.25f), 0.3f);

        yield return new WaitForSeconds(duration);

        specialStunned = false;
        SetGlow(new Color(0.45f, 0.60f, 0.85f), 0.6f);
    }

    // Brief zoom-out on berserk start so the player can see the whole arena
    IEnumerator BerserkCameraZoom()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        float origSize  = cam.orthographicSize;
        float zoomOut   = origSize * 1.35f;
        float zoomDur   = 0.35f;
        float holdDur   = 0.5f;
        float restoreDur = 0.5f;

        // Zoom out
        float t = 0f;
        while (t < zoomDur)
        {
            t += Time.deltaTime;
            cam.orthographicSize = Mathf.Lerp(origSize, zoomOut, Mathf.SmoothStep(0f, 1f, t / zoomDur));
            yield return null;
        }

        yield return new WaitForSeconds(holdDur);

        // Restore
        t = 0f;
        while (t < restoreDur)
        {
            t += Time.deltaTime;
            cam.orthographicSize = Mathf.Lerp(zoomOut, origSize, Mathf.SmoothStep(0f, 1f, t / restoreDur));
            yield return null;
        }
        cam.orthographicSize = origSize;
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
        bullet.tag = "EnemyBullet";
    }

    // ── Phase 2 Sweep ─────────────────────────────────────────────────────────

    IEnumerator SweepAttack()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;

        var overlay = BuildSweepOverlay();
        var warnUI  = BuildSweepWarningUI();

        SetGlow(new Color(0.85f, 0.85f, 0.85f), 5f);

        float elapsed = 0f;
        while (elapsed < sweepWarningDuration)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.PingPong(elapsed * 3.5f, 1f);

            if (overlay != null)
            {
                var sr  = overlay.GetComponent<SpriteRenderer>();
                sr.color = new Color(0.65f, 0.65f, 0.65f, Mathf.Lerp(0.08f, 0.40f, pulse));
            }

            if (warnUI != null)
            {
                float alpha = Mathf.Lerp(0.4f, 1f, pulse);
                UpdateSweepWarningAlpha(warnUI, alpha);
            }

            yield return null;
        }

        if (overlay != null)
            overlay.GetComponent<SpriteRenderer>().color = new Color(0.85f, 0.85f, 0.85f, 0.70f);

        if (playerTransform != null)
        {
            bool safe = playerMovement != null && playerMovement.IsDashing;
            if (!safe)
                playerTransform.GetComponent<PlayerHealth>()?.TakeDamage(sweepDamageToPlayer);
        }

        yield return new WaitForSeconds(0.15f);

        if (warnUI != null) Destroy(warnUI);

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

    GameObject BuildSweepWarningUI()
    {
        var canvasObj = new GameObject("SweepWarnCanvas");
        var canvas    = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        var scaler                 = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        CreateEdgePanel(canvasObj, "Top",    new Vector2(0.5f, 1f), new Vector2(1f, 0f),   new Vector2(0f, -8f),  new Vector2(1920f, 18f));
        CreateEdgePanel(canvasObj, "Bottom", new Vector2(0.5f, 0f), new Vector2(1f, 1f),   new Vector2(0f,  8f),  new Vector2(1920f, 18f));
        CreateEdgePanel(canvasObj, "Left",   new Vector2(0f,  0.5f), new Vector2(1f, 0.5f), new Vector2( 8f, 0f), new Vector2(18f, 1080f));
        CreateEdgePanel(canvasObj, "Right",  new Vector2(1f,  0.5f), new Vector2(0f, 0.5f), new Vector2(-8f, 0f), new Vector2(18f, 1080f));

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

    // ── Phase 2 Entry Effects ─────────────────────────────────────────────────

    IEnumerator Phase2EntryEffects()
    {
        // Sustained ground shake
        StartCoroutine(Phase2GroundShake());

        // Show "!!!" warning UI
        var warnUI = BuildPhase2EntryUI();

        // Camera: brief zoom to boss then back
        StartCoroutine(Phase2EntryZoom());

        // Red ground crack ripple
        StartCoroutine(Phase2GroundRipple());

        yield return new WaitForSeconds(2.0f);

        if (warnUI != null) Destroy(warnUI);
    }

    IEnumerator Phase2GroundShake()
    {
        float dur = 2.5f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float intensity = Mathf.Lerp(0.4f, 0.1f, elapsed / dur);
            CameraShake.Instance?.Shake(0.08f, intensity);
            yield return new WaitForSeconds(0.08f);
        }
    }

    // Expanding dark-red rings from boss position — like cracks in the floor
    IEnumerator Phase2GroundRipple()
    {
        int waves = 4;
        for (int w = 0; w < waves; w++)
        {
            StartCoroutine(SpawnGroundRing(0.3f + w * 0.12f));
            yield return new WaitForSeconds(0.22f);
        }
    }

    IEnumerator SpawnGroundRing(float delay)
    {
        yield return new WaitForSeconds(delay);

        var shader = Shader.Find("Sprites/Default");
        if (shader == null) yield break;

        const int seg = 36;
        var go = new GameObject("Phase2GroundRing");
        var lr = go.AddComponent<LineRenderer>();
        lr.material      = new Material(shader);
        lr.loop          = true;
        lr.positionCount = seg;
        lr.sortingOrder  = 3;

        float maxR   = roomHalfSize.x * 1.8f;
        float dur    = 0.9f;
        float elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t      = elapsed / dur;
            float radius = maxR * t;
            float alpha  = 1f - t;
            float width  = Mathf.Lerp(0.5f, 0.1f, t);

            lr.startWidth = width; lr.endWidth = width;
            Color c = new Color(0.8f, 0.1f, 0.1f, alpha);
            lr.startColor = c; lr.endColor = c;

            Vector2 center = transform.position;
            for (int i = 0; i < seg; i++)
            {
                float angle = (float)i / seg * 2f * Mathf.PI;
                lr.SetPosition(i, new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius, 0f));
            }
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator Phase2EntryZoom()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        float origSize  = cam.orthographicSize;
        float zoomIn    = origSize * 0.72f;
        float zoomDur   = 0.40f;
        float holdDur   = 0.55f;
        float restoreDur = 0.60f;

        // Quick zoom in on the boss
        float t = 0f;
        while (t < zoomDur)
        {
            t += Time.deltaTime;
            cam.orthographicSize = Mathf.Lerp(origSize, zoomIn, Mathf.SmoothStep(0f, 1f, t / zoomDur));
            yield return null;
        }

        yield return new WaitForSeconds(holdDur);

        // Zoom back out
        t = 0f;
        while (t < restoreDur)
        {
            t += Time.deltaTime;
            cam.orthographicSize = Mathf.Lerp(zoomIn, origSize, Mathf.SmoothStep(0f, 1f, t / restoreDur));
            yield return null;
        }
        cam.orthographicSize = origSize;
    }

    // "!!!" overlay shown on screen when phase 2 begins
    GameObject BuildPhase2EntryUI()
    {
        var canvasObj = new GameObject("Phase2EntryCanvas");
        var canvas    = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 160;

        var scaler                 = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // "!!!" large red text in center
        var textObj = new GameObject("Phase2Text");
        textObj.transform.SetParent(canvasObj.transform, false);
        var textRT          = textObj.AddComponent<RectTransform>();
        textRT.anchorMin    = new Vector2(0.5f, 0.5f);
        textRT.anchorMax    = new Vector2(0.5f, 0.5f);
        textRT.sizeDelta    = new Vector2(800f, 180f);
        textRT.anchoredPosition = Vector2.zero;
        var text            = textObj.AddComponent<Text>();
        text.text           = "!!!";
        text.fontSize       = 120;
        text.fontStyle      = FontStyle.Bold;
        text.alignment      = TextAnchor.MiddleCenter;
        text.color          = new Color(1f, 0.08f, 0.08f, 1f);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null) text.font = font;

        // Sub-text
        var subObj = new GameObject("Phase2SubText");
        subObj.transform.SetParent(canvasObj.transform, false);
        var subRT           = subObj.AddComponent<RectTransform>();
        subRT.anchorMin     = new Vector2(0.5f, 0.5f);
        subRT.anchorMax     = new Vector2(0.5f, 0.5f);
        subRT.sizeDelta     = new Vector2(700f, 80f);
        subRT.anchoredPosition = new Vector2(0f, -90f);
        var subText         = subObj.AddComponent<Text>();
        subText.text        = "ISSHIN AWAKENS";
        subText.fontSize    = 42;
        subText.fontStyle   = FontStyle.BoldAndItalic;
        subText.alignment   = TextAnchor.MiddleCenter;
        subText.color       = new Color(0.95f, 0.85f, 0.85f, 0.9f);
        if (font != null) subText.font = font;

        // Pulse the text via coroutine on the canvas
        StartCoroutine(PulsePhase2UI(canvasObj));

        return canvasObj;
    }

    IEnumerator PulsePhase2UI(GameObject ui)
    {
        if (ui == null) yield break;
        float elapsed = 0f;
        float dur     = 2.0f;
        while (elapsed < dur && ui != null)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.PingPong(elapsed * 4f, 1f);
            float alpha = Mathf.Lerp(0.5f, 1f, pulse) * Mathf.Clamp01(1f - (elapsed - 1.4f) / 0.6f);
            if (ui != null) UpdateSweepWarningAlpha(ui, alpha);
            yield return null;
        }
    }

    // ── Contact Damage / Trigger ──────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        if (state == BossState.Dead) return;

        if (state == BossState.Berserk && other.CompareTag("Player"))
        {
            other.GetComponent<PlayerHealth>()?.TakeDamage(berserkContactDamage);
            return;
        }

        // Flash (L key) during berserk → stun boss
        if (state == BossState.Berserk
            && other.CompareTag("LightSource")
            && other.GetComponent<LightWaveFader>() != null
            && !other.GetComponent<SlashBulletDeflector>()
            && !berserkStunned)
        {
            StartCoroutine(BerserkFlashStun());
            return;
        }

        if (other.CompareTag("LightSource") && other.GetComponent<SlashBulletDeflector>() != null)
            ApplyDamage(slashDamageOnBoss);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (state == BossState.Dead || state != BossState.Berserk) return;
        if (berserkStunned) return;
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

        // Trigger phase 2 entry effects the first time health crosses the threshold
        if (!phase2EffectsPlayed && health <= maxHealth * 0.40f)
        {
            phase2EffectsPlayed = true;
            StartCoroutine(Phase2EntryEffects());
        }

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
        // If player is dashing, they dodge the hit (handled by parent boss for bonus damage)
        var pm = other.GetComponent<PlayerMovement>();
        if (pm != null && pm.IsDashing) return;
        hasHit = true;
        other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
    }
}
