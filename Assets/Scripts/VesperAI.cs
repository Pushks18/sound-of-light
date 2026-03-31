using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Boss AI for Vesper.
///
/// COLLIDER SETUP NOTE:
///   Vesper uses only TRIGGER colliders (isTrigger = true) — no solid body.
///   This is intentional: Bullet.cs returns early when it hits a trigger,
///   so bullets pass through to VesperAI.OnTriggerEnter2D which handles
///   damage and destroys the bullet. Slash is detected via the SlashBulletDeflector
///   component present on every slash light object.
///
/// INVINCIBILITY NOTE:
///   Damage checks use the value of isCurrentlyLit at the moment of impact.
///   Physics callback order per fixed step is: Enter2D → Stay2D → FixedUpdate.
///   isCurrentlyLit is set by Stay2D and cleared at the end of FixedUpdate.
///   A bullet's own BulletLightTrigger child fires Stay2D *after* the bullet's
///   Enter2D, so a bullet arriving in a dark room finds isCurrentlyLit = false
///   and is blocked. Only persistent LightSources (the L-flash) that were
///   already overlapping from a previous frame keep isCurrentlyLit = true
///   when a bullet or slash arrives.
/// </summary>
public class VesperAI : MonoBehaviour
{
    // ── Inspector Parameters ─────────────────────────────────────────────────

    [Header("Stats")]
    [SerializeField] float maxHealth          = 15f;
    [SerializeField] int   phase2HPThreshold  = 8;

    [Header("Scatter Attack")]
    [SerializeField] float scatterIntervalWhileLit  = 4f;
    [SerializeField] float phase2ScatterInterval    = 3f;
    [SerializeField] int   scatterBulletCount       = 10;
    [SerializeField] int   phase2ScatterBulletCount = 14;

    [Header("Damage")]
    [SerializeField] float slashDamage  = 2f;
    [SerializeField] float bulletDamage = 0.5f;

    [Header("Vulnerable Window")]
    [SerializeField] float vulnerableWindow = 5f;  // seconds Vesper can be damaged after being lit

    [Header("Slash Stagger")]
    [SerializeField] float slashStaggerDuration    = 2f;
    [SerializeField] float slashTeleportThreshold  = 10f;  // forced teleport after this much slash damage per cycle

    [Header("Teleport")]
    [SerializeField] float teleportMeleeRadius    = 2f;
    [SerializeField] float teleportMeleeDuration  = 3f;
    [SerializeField] int   teleportKHitThreshold  = 3;
    [SerializeField] int   phase2TeleportKHitThreshold = 2;
    [SerializeField] int   teleportDashHitThreshold = 2;
    [SerializeField] float teleportFadeDuration   = 0.5f;
    [SerializeField] float teleportEyeRestoreDelay = 0.3f;

    [Header("Visibility Fade")]
    [SerializeField] float alphaFadeInDuration  = 0.2f;
    [SerializeField] float alphaFadeOutDuration = 0.3f;

    [Header("Death")]
    [SerializeField] float deathEyeExpandRadius   = 5f;
    [SerializeField] float deathEyeExpandDuration = 0.3f;
    [SerializeField] float deathEyeFadeDuration   = 1f;    // how long eyes take to fade out after radius collapse
    [SerializeField] float deathFadeDelay         = 2f;    // total time before camera pan + BossDefeated()

    [Header("References")]
    [SerializeField] GameObject enemyBulletPrefab;  // EnemyBullet prefab
    [SerializeField] public Light2D eyeLeft;
    [SerializeField] public Light2D eyeRight;
    [SerializeField] BossHealthBar healthBar;
    [SerializeField] BossIntroCam  bossIntroCam;

    [Header("Room Bounds (for teleport sampling)")]
    [SerializeField] Vector2 roomCenter   = Vector2.zero;
    [SerializeField] Vector2 roomHalfSize = new Vector2(12f, 12f);

    // ── State ────────────────────────────────────────────────────────────────

    enum BossState { Dormant, Intro, InBattle, Dead }
    BossState state = BossState.Dormant;

    float        health;
    bool         isCurrentlyLit    = false;
    bool         wasLitLastFrame   = false;
    bool         activationDone    = false;   // one-shot activation sequence guard

    bool         isTeleporting     = false;
    bool         inSlashStagger    = false;
    float        slashStaggerTimer = 0f;
    bool         slashStaggerAvailable = true;  // resets after each teleport

    int          kHitCount              = 0;
    int          dashHitCount           = 0;
    float        slashDamageThisCycle   = 0f;
    float        scatterTimer           = 0f;
    float        meleeProximityTimer    = 0f;
    float        vulnerableTimer        = 0f;  // counts down from vulnerableWindow

    // Alpha fade state (Update-driven, coroutine-free so teleport coroutine can also set alpha)
    float        currentAlpha   = 0f;
    float        alphaTarget    = 0f;
    float        alphaSpeed     = 5f;   // units per second (1 / duration)


    SpriteRenderer   sr;            // main body renderer — also used by HitFlash for white tint
    Color            spriteBaseColor = Color.white;
    SpriteRenderer[] allRenderers;  // all child SpriteRenderers (body + eye dots)
    Color[]          rendererBaseColors;
    Transform        playerTransform;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        health = maxHealth;
        sr = GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            spriteBaseColor = sr.color;
            spriteBaseColor.a = 1f;
        }

        // Collect body SpriteRenderers only — exclude eye GameObjects so their
        // sprites (white dots) are always visible regardless of body alpha.
        var all = GetComponentsInChildren<SpriteRenderer>(true);
        var body = new System.Collections.Generic.List<SpriteRenderer>();
        foreach (var rend in all)
        {
            bool isEye = (eyeLeft  != null && rend.transform.IsChildOf(eyeLeft.transform))
                      || (eyeRight != null && rend.transform.IsChildOf(eyeRight.transform));
            if (!isEye) body.Add(rend);
        }
        allRenderers = body.ToArray();
        rendererBaseColors = new Color[allRenderers.Length];
        for (int i = 0; i < allRenderers.Length; i++)
        {
            var c = allRenderers[i].color;
            c.a = 1f;
            rendererBaseColors[i] = c;
        }

        ApplyAlpha(0f);   // start invisible

        // Health bar is initialized in ActivationSequence (after all Awake/Start have run)
        // to avoid Unity Awake-ordering issues where fillImage may not yet exist.
    }

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    // Guards against multiple arena triggers firing in the same physics frame
    private bool introScheduled = false;

    /// <summary>
    /// Called by the FIRST VesperArenaTrigger that detects the player.
    /// Returns true only once — subsequent calls return false so other triggers
    /// do not start a second intro sequence.
    /// </summary>
    public bool TryScheduleIntro()
    {
        if (introScheduled) return false;
        introScheduled = true;
        return true;
    }

    /// <summary>
    /// Called by VesperArenaTrigger after the camera returns to the player.
    /// Transitions Vesper into idle-waiting (Intro) state and starts the blink loop.
    /// Eyes should already be at eyeIntensityNormal from the inline animation in the trigger.
    /// </summary>
    public void StartIntroSequence()
    {
        if (state != BossState.Dormant) return;
        state           = BossState.Intro;
        isCurrentlyLit  = false;   // discard any light contact that happened before the trigger
        wasLitLastFrame = false;
    }

    // ── Update ───────────────────────────────────────────────────────────────

    void Update()
    {
        if (state == BossState.Dormant || state == BossState.Dead) return;

        // Continuously drive alpha based on current lit state (not transition-based)
        // Skip during teleport — TeleportSequence manages alpha itself
        if (!isTeleporting)
        {
            float desired = wasLitLastFrame ? bodyMaxAlpha : bodyMinAlpha;
            float speed   = wasLitLastFrame ? alphaFadeInDuration : alphaFadeOutDuration;
            SetAlphaTarget(desired, speed);
        }
        UpdateAlphaFade();

        // Vulnerable window countdown
        if (vulnerableTimer > 0f)
            vulnerableTimer -= Time.deltaTime;

        if (state != BossState.InBattle) return;

        // Slash stagger countdown
        if (inSlashStagger)
        {
            slashStaggerTimer -= Time.deltaTime;
            if (slashStaggerTimer <= 0f)
                inSlashStagger = false;
        }

        // Melee proximity teleport trigger
        if (!isTeleporting && playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist < teleportMeleeRadius)
            {
                meleeProximityTimer += Time.deltaTime;
                if (meleeProximityTimer >= teleportMeleeDuration)
                    StartCoroutine(TeleportSequence());
            }
            else
            {
                meleeProximityTimer = 0f;
            }
        }
    }

    void FixedUpdate()
    {
        if (state == BossState.Dormant || state == BossState.Dead) return;

        // Detect light transitions (Enter2D → Stay2D → FixedUpdate order is guaranteed)
        bool justBecameLit   = isCurrentlyLit && !wasLitLastFrame;
        bool justBecameUnlit = !isCurrentlyLit && wasLitLastFrame;

        if (justBecameLit)   OnBecameLit();
        if (justBecameUnlit) OnBecameUnlit();

        // Scatter cooldown while lit and in battle
        if (isCurrentlyLit && state == BossState.InBattle && !inSlashStagger && !isTeleporting)
        {
            scatterTimer -= Time.fixedDeltaTime;
            if (scatterTimer <= 0f)
            {
                FireScatter();
                scatterTimer = IsPhase2 ? phase2ScatterInterval : scatterIntervalWhileLit;
            }
        }

        // Carry current lit state forward, then clear for next frame
        wasLitLastFrame = isCurrentlyLit;
        isCurrentlyLit  = false;  // re-set by OnTriggerStay2D if still in light
    }

    // ── Light Transition Handlers ────────────────────────────────────────────

    [Header("Body Opacity")]
    [SerializeField] float bodyMinAlpha = 0.15f;  // always faintly visible (ghost presence)
    [SerializeField] float bodyMaxAlpha = 0.45f;  // when lit — still semi-transparent

    void OnBecameLit()
    {
        // Start vulnerable window
        vulnerableTimer = vulnerableWindow;

        if (state == BossState.Intro && !activationDone)
        {
            // First time lit — trigger one-shot activation sequence
            activationDone = true;
            StartCoroutine(ActivationSequence());
            return;
        }

        if (state == BossState.InBattle)
        {
            // Immediate scatter on every new illumination
            FireScatter();
            scatterTimer = IsPhase2 ? phase2ScatterInterval : scatterIntervalWhileLit;
        }
    }

    void OnBecameUnlit() { }

    // ── Trigger Callbacks ────────────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        if (state == BossState.Dormant || state == BossState.Dead) return;

        // Player K-bullet hit
        // Note: Bullet.cs returns early for trigger colliders, so the bullet
        // does NOT self-destruct — VesperAI must destroy it manually.
        if (other.CompareTag("Bullet"))
        {
            HandleBulletHit(other);
            return;
        }

        // Slash hit — identified by SlashBulletDeflector component on the slash light object
        if (other.CompareTag("LightSource") && other.GetComponent<SlashBulletDeflector>() != null)
        {
            HandleSlashHit();
            return;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (state == BossState.Dormant || state == BossState.Dead) return;

        // Only the player's L-flash (LightWaveFader component) illuminates Vesper.
        // Bullets, slash arcs, and dash trails are excluded.
        if (other.CompareTag("LightSource") && other.GetComponent<LightWaveFader>() != null)
            isCurrentlyLit = true;
    }

    // ── Damage Handlers ──────────────────────────────────────────────────────

    void HandleBulletHit(Collider2D bulletCollider)
    {
        // Only vulnerable while illuminated.
        // wasLitLastFrame is used instead of isCurrentlyLit alone because Enter2D fires
        // before Stay2D in the same physics step — so isCurrentlyLit is always false at
        // bullet-hit time even when a persistent LightSource (L-flash) is overlapping.
        // wasLitLastFrame = true means the light was confirmed overlapping last completed step.
        if (vulnerableTimer <= 0f) return;
        if (state != BossState.InBattle) return;

        Destroy(bulletCollider.gameObject);

        health -= bulletDamage;
        health  = Mathf.Max(health, 0f);
        kHitCount++;

        if (healthBar != null) healthBar.SetHealth(health);

        SpawnFloatingText(
            "-" + bulletDamage.ToString("F1"),
            transform.position,
            new Color(0.65f, 0.65f, 0.65f));

        StartCoroutine(HitFlash());

        // K-hit teleport threshold
        int threshold = IsPhase2 ? phase2TeleportKHitThreshold : teleportKHitThreshold;
        if (kHitCount >= threshold && !isTeleporting)
            StartCoroutine(TeleportSequence());

        if (health <= 0f) StartCoroutine(DeathSequence());
    }

    public void TakeDashDamage(float amount)
    {
        if (vulnerableTimer <= 0f) return;
        if (state != BossState.InBattle) return;

        health -= amount;
        health  = Mathf.Max(health, 0f);
        dashHitCount++;

        if (healthBar != null) healthBar.SetHealth(health);
        DamageNumber.Spawn((int)amount, transform.position);
        StartCoroutine(HitFlash());

        if (health <= 0f) { StartCoroutine(DeathSequence()); return; }

        if (dashHitCount >= teleportDashHitThreshold && !isTeleporting)
            StartCoroutine(TeleportSequence());
    }

    void HandleSlashHit()
    {
        if (vulnerableTimer <= 0f) return;
        if (state != BossState.InBattle) return;

        health -= slashDamage;
        health  = Mathf.Max(health, 0f);
        slashDamageThisCycle += slashDamage;

        if (healthBar != null) healthBar.SetHealth(health);

        DamageNumber.Spawn((int)slashDamage, transform.position);

        // Stagger — only once per teleport cycle
        if (slashStaggerAvailable)
        {
            slashStaggerAvailable = false;
            inSlashStagger        = true;
            slashStaggerTimer     = slashStaggerDuration;
        }

        if (health <= 0f) { StartCoroutine(DeathSequence()); return; }

        // Forced teleport once slash damage this cycle exceeds threshold
        if (slashDamageThisCycle >= slashTeleportThreshold && !isTeleporting)
            StartCoroutine(TeleportSequence());
    }

    // ── Coroutines ───────────────────────────────────────────────────────────

    IEnumerator ActivationSequence()
    {
        // Health bar: initialize with correct maxHealth, then fade in.
        // Done here (not in Awake) so BuildUI() has definitely already run
        // and fillImage is not null. Also guarantees maxHealth != 0 so
        // SetFill(hp / maxHealth) produces the correct fraction.
        if (healthBar != null)
        {
            healthBar.Initialize(maxHealth);
            healthBar.Show();
        }

        yield return new WaitForSeconds(0.2f);

        state = BossState.InBattle;
        FireScatter();
        scatterTimer = IsPhase2 ? phase2ScatterInterval : scatterIntervalWhileLit;
    }

    void FireScatter()
    {
        if (enemyBulletPrefab == null || inSlashStagger || isTeleporting) return;

        int   count     = IsPhase2 ? phase2ScatterBulletCount : scatterBulletCount;
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            // Angle from east (Unity convention): 0 = right, 90 = up
            float angleDeg = angleStep * i;
            // Bullets move in transform.up, so rotation = angleDeg - 90
            float rotation = angleDeg - 90f;
            Instantiate(enemyBulletPrefab, transform.position, Quaternion.Euler(0f, 0f, rotation));
        }
    }

    IEnumerator TeleportSequence()
    {
        if (isTeleporting) yield break;
        isTeleporting = true;

        // 1. Fade out sprite over teleportFadeDuration
        SetAlphaTarget(0f, teleportFadeDuration);
        yield return new WaitForSeconds(teleportFadeDuration);

        // 2. Teleport to best position
        transform.position = FindTeleportPosition();

        // 3. Brief pause
        yield return new WaitForSeconds(teleportEyeRestoreDelay);

        // 4. Reset per-cycle counters
        kHitCount             = 0;
        dashHitCount          = 0;
        slashDamageThisCycle  = 0f;
        meleeProximityTimer   = 0f;
        slashStaggerAvailable = true;
        inSlashStagger        = false;
        vulnerableTimer       = 0f;  // back to invincible after teleport

        isTeleporting = false;

        // Snap alpha to correct state — both currentAlpha and alphaTarget must match
        // or UpdateAlphaFade will immediately pull alpha back to the old target
        ApplyAlpha(bodyMinAlpha);
        SetAlphaTarget(bodyMinAlpha, 0.01f);
    }

    Vector2 FindTeleportPosition()
    {
        if (playerTransform == null) return roomCenter;

        Vector2 playerPos = playerTransform.position;
        float   bestDist  = -1f;
        Vector2 bestPos   = roomCenter;

        for (int i = 0; i < 10; i++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(roomCenter.x - roomHalfSize.x * 0.8f,
                             roomCenter.x + roomHalfSize.x * 0.8f),
                Random.Range(roomCenter.y - roomHalfSize.y * 0.8f,
                             roomCenter.y + roomHalfSize.y * 0.8f));

            // Skip positions that are currently inside a LightSource collider
            bool inLight = false;
            var hits = Physics2D.OverlapCircleAll(candidate, 0.3f);
            foreach (var h in hits)
            {
                if (h.CompareTag("LightSource")) { inLight = true; break; }
            }
            if (inLight) continue;

            float dist = Vector2.Distance(candidate, playerPos);
            if (dist > bestDist)
            {
                bestDist = dist;
                bestPos  = candidate;
            }
        }

        return bestPos;
    }

    IEnumerator DeathSequence()
    {
        if (state == BossState.Dead) yield break;
        state         = BossState.Dead;
        isTeleporting = true;  // prevent any further teleport coroutines

        // 0. Lock camera on boss for the death animation
        bossIntroCam?.FocusOnBoss(transform.position);

        // 1. Full-screen white flash (0.3 s)
        StartCoroutine(FullScreenWhiteFlash(deathEyeExpandDuration));

        // 2. Eyes expand radius then collapse
        float startRadius = eyeLeft != null ? eyeLeft.pointLightOuterRadius : 1f;

        // Expand phase
        float elapsed = 0f;
        while (elapsed < deathEyeExpandDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (deathEyeExpandDuration * 0.5f);
            SetEyeRadius(Mathf.Lerp(startRadius, deathEyeExpandRadius, t));
            yield return null;
        }

        // Collapse phase
        elapsed = 0f;
        while (elapsed < deathEyeExpandDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (deathEyeExpandDuration * 0.5f);
            SetEyeRadius(Mathf.Lerp(deathEyeExpandRadius, startRadius, t));
            yield return null;
        }

        // Eye intensity fade to 0
        float startEyeIntensity = eyeLeft != null ? eyeLeft.intensity : 0f;
        elapsed = 0f;
        while (elapsed < deathEyeFadeDuration)
        {
            elapsed += Time.deltaTime;
            SetEyeIntensity(Mathf.Lerp(startEyeIntensity, 0f, elapsed / deathEyeFadeDuration));
            yield return null;
        }
        SetEyeIntensity(0f);

        // 3. Fade sprite out over deathFadeDelay * 0.5 s
        float fadeDur = deathFadeDelay * 0.5f;
        elapsed = 0f;
        while (elapsed < fadeDur)
        {
            elapsed += Time.deltaTime;
            ApplyAlpha(Mathf.Lerp(currentAlpha, 0f, elapsed / fadeDur));
            yield return null;
        }
        ApplyAlpha(0f);

        // 4. Wait remainder
        yield return new WaitForSeconds(deathFadeDelay - fadeDur);

        // 5. Smoothly pan camera back to player, then trigger victory
        if (bossIntroCam != null && playerTransform != null)
            yield return StartCoroutine(bossIntroCam.PanBackToPlayer(playerTransform));

        GameManager.Instance?.BossDefeated();
    }

    IEnumerator FullScreenWhiteFlash(float duration)
    {
        // Instantiate a temporary full-screen white canvas
        var canvasObj = new GameObject("VesperDeathFlash");
        var canvas    = canvasObj.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        var panelObj = new GameObject("WhitePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        var rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
        var img = panelObj.AddComponent<Image>();
        img.color          = Color.white;
        img.raycastTarget  = false;

        yield return new WaitForSeconds(duration);
        Destroy(canvasObj);
    }

    IEnumerator HitFlash()
    {
        // Brief white sprite tint on bullet hit (no knockback, no alpha change)
        if (sr == null) yield break;

        // Flash to white while preserving current alpha
        var c = sr.color;
        sr.color = new Color(1f, 1f, 1f, c.a);
        yield return new WaitForSeconds(0.05f);
        // Restore base color with current alpha
        sr.color = new Color(spriteBaseColor.r, spriteBaseColor.g, spriteBaseColor.b, currentAlpha);
    }

    // ── Alpha Helpers ────────────────────────────────────────────────────────

    /// <summary>Set a new alpha target. alphaSpeed = 1/duration units per second.</summary>
    void SetAlphaTarget(float target, float duration)
    {
        alphaTarget = target;
        alphaSpeed  = duration > 0.0001f ? 1f / duration : 1000f;
    }

    void UpdateAlphaFade()
    {
        if (Mathf.Abs(currentAlpha - alphaTarget) < 0.001f) return;
        currentAlpha = Mathf.MoveTowards(currentAlpha, alphaTarget, alphaSpeed * Time.deltaTime);
        ApplyAlpha(currentAlpha);
    }

    /// <summary>Directly write alpha to all child SpriteRenderers (also updates currentAlpha).</summary>
    void ApplyAlpha(float a)
    {
        currentAlpha = a;
        if (allRenderers == null) return;
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] == null) continue;
            var c = rendererBaseColors[i];
            c.a = a;
            allRenderers[i].color = c;
        }
    }

    // ── Eye Helpers ──────────────────────────────────────────────────────────

    void SetEyeIntensity(float intensity)
    {
        if (eyeLeft  != null) eyeLeft.intensity  = intensity;
        if (eyeRight != null) eyeRight.intensity = intensity;
    }

    void SetEyeRadius(float radius)
    {
        if (eyeLeft  != null) eyeLeft.pointLightOuterRadius  = radius;
        if (eyeRight != null) eyeRight.pointLightOuterRadius = radius;
    }

    // ── Utility ──────────────────────────────────────────────────────────────

    bool IsPhase2 => health <= phase2HPThreshold;

    /// <summary>Used by BossIntroCam to decide whether to glance at boss on enemy death.</summary>
    public bool IsInBattle => state == BossState.InBattle;

    /// <summary>
    /// Spawns a floating text label in world space.
    /// Used for the gray "-0.5" bullet damage numbers that DamageNumber.Spawn()
    /// cannot produce (it only accepts int and uses a fixed red color).
    /// </summary>
    static void SpawnFloatingText(string text, Vector3 worldPos, Color color)
    {
        var go = new GameObject("VesperDmgText");

        float xOff = Random.Range(-0.25f, 0.25f);
        float yOff = Random.Range(0.4f,   0.7f);
        go.transform.position = worldPos + new Vector3(xOff, yOff, 0f);

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text         = text;
        tmp.fontSize     = 6f;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.color        = color;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(0, 0, 0, 180);
        tmp.sortingOrder = 100;

        go.AddComponent<FloatingTextFader>();
    }
}

/// <summary>
/// Floats a TextMeshPro upward and fades it out.
/// Used by VesperAI.SpawnFloatingText. Must be in the same file because
/// it is only used here and does not need to be a separate asset.
/// </summary>
public class FloatingTextFader : MonoBehaviour
{
    private const float FloatSpeed = 1.5f;
    private const float Lifetime   = 1.5f;

    private TextMeshPro tmp;
    private float elapsed;

    void Start()
    {
        tmp = GetComponent<TextMeshPro>();
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += Vector3.up * FloatSpeed * Time.deltaTime;

        float t    = elapsed / Lifetime;
        float fade = t < 0.4f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.4f) / 0.6f);
        float scale = t < 0.15f
            ? Mathf.Lerp(1.3f, 1f, t / 0.15f)
            : Mathf.Lerp(1f, 0.7f, (t - 0.15f) / 0.85f);

        if (tmp != null)
        {
            var c = tmp.color;
            c.a       = fade;
            tmp.color = c;
        }

        transform.localScale = Vector3.one * scale;

        if (elapsed >= Lifetime)
            Destroy(gameObject);
    }
}
