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
    // ── Inspector Parameters (gameplay only) ────────────────────────────────

    [Header("Stats")]
    [SerializeField] float maxHealth         = 15f;
    [SerializeField] int   phase2HPThreshold = 8;

    [Header("Scatter Attack")]
    [SerializeField] float scatterIntervalWhileLit  = 4f;
    [SerializeField] float phase2ScatterInterval    = 3f;
    [SerializeField] int   scatterBulletCount       = 10;
    [SerializeField] int   phase2ScatterBulletCount = 14;

    [Header("Damage")]
    [SerializeField] float slashDamage  = 2f;
    [SerializeField] float bulletDamage = 0.5f;

    [Header("Vulnerable Window")]
    [SerializeField] float vulnerableWindow = 5f;

    [Header("Slash Stagger")]
    [SerializeField] float slashStaggerDuration   = 2f;
    [SerializeField] float slashTeleportThreshold = 10f;

    [Header("Teleport Thresholds")]
    [SerializeField] float teleportMeleeRadius         = 2f;
    [SerializeField] float teleportMeleeDuration       = 3f;
    [SerializeField] int   teleportKHitThreshold       = 3;
    [SerializeField] int   phase2TeleportKHitThreshold = 2;
    [SerializeField] int   teleportDashHitThreshold    = 2;

    [Header("References")]
    [SerializeField] GameObject  enemyBulletPrefab;
    [SerializeField] public Light2D eyeLeft;
    [SerializeField] public Light2D eyeRight;
    [SerializeField] Light2D     globalLight;   // scene's Global Light 2D
    [SerializeField] BossHealthBar healthBar;
    [SerializeField] BossIntroCam  bossIntroCam;

    [Header("Room Bounds (for teleport sampling)")]
    [SerializeField] Vector2 roomCenter   = Vector2.zero;
    [SerializeField] Vector2 roomHalfSize = new Vector2(12f, 12f);

    [Header("Body Opacity")]
    [SerializeField] float bodyMinAlpha = 0.15f;
    [SerializeField] float bodyMaxAlpha = 0.45f;

    // ── Animation / Cinematic Parameters (hardcoded — not Inspector-editable) ─

    // Visibility fade
    float alphaFadeInDuration  = 0.2f;
    float alphaFadeOutDuration = 0.3f;

    // Combat teleport visuals
    float teleportFadeDuration    = 0.5f;
    float teleportEyeRestoreDelay = 0.3f;
    float teleportArrivalDuration = 0.5f;

    // Death scramble
    float deathRetreatRoomFraction     = 0.55f;
    float deathBodyFadeDuration        = 1f;
    float deathHoldDuration            = 0.4f;
    int   deathTeleportCount           = 5;
    float deathEyeIntensityDecayPerHop = 0.6f;
    float deathHopFadeDuration         = 0.12f;
    float deathHopEyeRestoreDelay      = 0.05f;
    float deathHopPauseDuration        = 0.2f;   // slightly longer than before
    float deathPortalDelay             = 1.5f;

    // Phase 2 transition
    int   phase2TeleportCount         = 3;
    float phase2TransitionDelay       = 0.5f;
    float phase2TeleportInterval      = 0.7f;
    Color phase2EyeColor              = new Color(1f, 0.15f, 0.1f, 1f);
    float phase2EyeColorFadeDuration  = 0.35f;
    float phase2TransitionZigzagOffset = 4f;

    // Eye effects
    float eyeGhostFadeDuration        = 0.5f;
    float eyeArrivalFlashMultiplier   = 4f;
    float eyeArrivalFlashDuration     = 0.35f;
    float phase2ArrivalBurstRadius    = 14f;
    float phase2ArrivalBurstDuration  = 0.6f;
    float phase2EyeIntensityMultiplier = 1.4f;
    float phase2DepartureShakeDuration = 0.25f;
    float phase2DepartureShakeAmplitude = 0.12f;

    // Phase 2 ambient light
    float phase2AmbientTargetIntensity = 0.12f;
    float phase2AmbientFadeInDuration  = 0.4f;
    float phase2AmbientFadeOutDuration = 0.8f;

    // ── State ────────────────────────────────────────────────────────────────

    enum BossState { Dormant, Intro, InBattle, Dead }
    BossState state = BossState.Dormant;

    float        health;
    bool         isCurrentlyLit    = false;
    bool         wasLitLastFrame   = false;
    bool         activationDone    = false;   // one-shot activation sequence guard
    bool         playerBuffApplied = false;

    bool         phase2Entered          = false;  // true once HP first drops to phase2 threshold
    bool         phase2SequenceTriggered = false;  // true once the post-phase2 teleport+scatter fires
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

    // Eye ghost tracking — lets DeathSequence destroy ghosts left over from interrupted coroutines
    readonly List<GameObject> _activeEyeGhosts = new List<GameObject>();

    // Alpha fade state (Update-driven, coroutine-free so teleport coroutine can also set alpha)
    float        currentAlpha   = 0f;
    float        alphaTarget    = 0f;
    float        alphaSpeed     = 5f;   // units per second (1 / duration)


    float            eyeNormalIntensity;  // intensity read from Inspector in Awake; eyes start at 0 and fade in on intro

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

        // Store eye intensity from Inspector then zero it out — eyes fade in during
        // the intro camera pan so the boss "awakens" on screen rather than being pre-lit.
        eyeNormalIntensity = eyeLeft != null ? eyeLeft.intensity : 1f;
        SetEyeIntensity(0f);

        // Health bar is initialized in ActivationSequence (after all Awake/Start have run)
        // to avoid Unity Awake-ordering issues where fillImage may not yet exist.
    }

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        StartCoroutine(ApplyStartupPlayerBuffNextFrame());
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
    /// Called by VesperArenaTrigger just before the intro camera pan starts.
    /// Eyes fade from 0 to their normal intensity after <paramref name="delay"/> seconds,
    /// so they light up exactly when the camera arrives at the boss.
    /// </summary>
    public void TriggerIntroEyeFade(float delay, float duration)
        => StartCoroutine(IntroEyeFadeIn(delay, duration));

    IEnumerator IntroEyeFadeIn(float delay, float duration)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetEyeIntensity(Mathf.Lerp(0f, eyeNormalIntensity, elapsed / duration));
            yield return null;
        }
        SetEyeIntensity(eyeNormalIntensity);
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
        if (isTeleporting) { Destroy(bulletCollider.gameObject); return; }  // already escaping

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
        TryTriggerPhase2Sequence();
        CheckPhase2Transition();

        // K-hit teleport threshold
        int threshold = IsPhase2 ? phase2TeleportKHitThreshold : teleportKHitThreshold;
        if (kHitCount >= threshold && !isTeleporting)
            StartCoroutine(TeleportSequence());

        if (health <= 0f) { StopAllCoroutines(); StartCoroutine(DeathSequence()); }
    }

    public void TakeDashDamage(float amount)
    {
        if (vulnerableTimer <= 0f) return;
        if (state != BossState.InBattle) return;
        if (isTeleporting) return;

        health -= amount;
        health  = Mathf.Max(health, 0f);
        dashHitCount++;

        if (healthBar != null) healthBar.SetHealth(health);
        DamageNumber.Spawn((int)amount, transform.position);
        StartCoroutine(HitFlash());
        TryTriggerPhase2Sequence();
        CheckPhase2Transition();

        if (health <= 0f) { StopAllCoroutines(); StartCoroutine(DeathSequence()); return; }

        if (dashHitCount >= teleportDashHitThreshold && !isTeleporting)
            StartCoroutine(TeleportSequence());
    }

    void HandleSlashHit()
    {
        if (vulnerableTimer <= 0f) return;
        if (state != BossState.InBattle) return;
        if (isTeleporting) return;  // already escaping — ignore hits during fade-out

        health -= slashDamage;
        health  = Mathf.Max(health, 0f);
        slashDamageThisCycle += slashDamage;

        if (healthBar != null) healthBar.SetHealth(health);

        DamageNumber.Spawn((int)slashDamage, transform.position);
        TryTriggerPhase2Sequence();
        CheckPhase2Transition();

        // Stagger — only once per teleport cycle
        if (slashStaggerAvailable)
        {
            slashStaggerAvailable = false;
            inSlashStagger        = true;
            slashStaggerTimer     = slashStaggerDuration;
        }

        if (health <= 0f) { StopAllCoroutines(); StartCoroutine(DeathSequence()); return; }

        // Forced teleport once slash damage this cycle exceeds threshold
        if (slashDamageThisCycle >= slashTeleportThreshold && !isTeleporting)
            StartCoroutine(TeleportSequence());
    }

    // ── Coroutines ───────────────────────────────────────────────────────────

    void CheckPhase2Transition()
    {
        if (phase2Entered) return;
        if (health <= 0f) return;   // death takes priority
        if (!IsPhase2) return;
        phase2Entered = true;
        StartCoroutine(Phase2TransitionSequence());
    }

    /// <summary>
    /// Called at the start of every damage handler (before CheckPhase2Transition).
    /// Fires the 3-teleport+scatter sequence on the first hit after phase 2 is entered.
    /// </summary>
    void TryTriggerPhase2Sequence()
    {
        if (!phase2Entered) return;           // not yet in phase 2
        if (phase2SequenceTriggered) return;  // already fired once
        if (isTeleporting) return;            // cinematic still running
        if (health <= 0f) return;             // death takes priority
        phase2SequenceTriggered = true;
        StartCoroutine(TeleportSequence());
    }

    IEnumerator Phase2TransitionSequence()
    {
        // Lock out normal behaviour during the cutscene
        isTeleporting  = true;
        inSlashStagger = false;

        // Pause player movement
        DisablePlayerInput();

        // Camera smoothly pans to boss
        if (bossIntroCam != null)
            yield return StartCoroutine(bossIntroCam.PanToBossPhase2(transform.position, 0.5f));

        // Health bar shake (1 s, strong) + eye colour fade — simultaneous
        if (healthBar != null) healthBar.Shake(1f, 14f);
        yield return StartCoroutine(FadeEyeColor(phase2EyeColor, phase2EyeColorFadeDuration));

        // Subtle brightness increase now that eyes are red — applied once, stays for the sequence
        float redIntensity = eyeLeft != null ? eyeLeft.intensity : 1f;
        SetEyeIntensity(redIntensity * phase2EyeIntensityMultiplier);

        float remainingDelay = Mathf.Max(0f, phase2TransitionDelay - phase2EyeColorFadeDuration);
        if (remainingDelay > 0f) yield return new WaitForSeconds(remainingDelay);

        // Fade ambient light up so the room is dimly visible — gives spatial reference during teleports
        StartCoroutine(FadeGlobalLight(phase2AmbientTargetIntensity, phase2AmbientFadeInDuration));

        // Pre-compute 3 zigzag positions: left → right → far destination
        Vector2[] zigzagPositions = FindPhase2TransitionPositions();

        // N cinematic teleports — no scatter, camera follows each hop
        for (int i = 0; i < Mathf.Min(phase2TeleportCount, zigzagPositions.Length); i++)
        {
            // Eyes jitter left-right as a departure windup, then ghost lingers at the old spot
            yield return StartCoroutine(ShakeEyesOnDeparture(phase2DepartureShakeDuration, phase2DepartureShakeAmplitude));
            SpawnEyeGhosts();

            SetAlphaTarget(0f, teleportFadeDuration);
            yield return new WaitForSeconds(teleportFadeDuration);

            transform.position = zigzagPositions[i];
            bossIntroCam?.SnapToPosition(transform.position);

            yield return new WaitForSeconds(teleportEyeRestoreDelay);

            // Snap visible — no flash, eyes stay at the slightly-boosted red intensity
            ApplyAlpha(bodyMaxAlpha);
            SetAlphaTarget(bodyMaxAlpha, 0.01f);

            yield return new WaitForSeconds(phase2TeleportInterval);
        }

        // Reset per-cycle counters so Phase 2 starts clean
        kHitCount             = 0;
        dashHitCount          = 0;
        slashDamageThisCycle  = 0f;
        meleeProximityTimer   = 0f;
        slashStaggerAvailable = true;
        inSlashStagger        = false;
        vulnerableTimer       = 0f;

        // Fade ambient light back to black while camera pans back — run concurrently
        StartCoroutine(FadeGlobalLight(0f, phase2AmbientFadeOutDuration));

        // Camera pans back to player, then player regains control
        if (bossIntroCam != null && playerTransform != null)
            yield return StartCoroutine(bossIntroCam.PanBackToPlayer(playerTransform));

        isTeleporting = false;
        // Body stays at bodyMaxAlpha from the last arrival — Update will fade it naturally.

        EnablePlayerInput();
    }

    /// <summary>
    /// Spawn fading ghost copies of both eyes at the current position.
    /// Call just before the boss starts teleporting so players see where it left from.
    /// </summary>
    void SpawnEyeGhosts()
    {
        if (eyeLeft  != null) SpawnSingleEyeGhost(eyeLeft);
        if (eyeRight != null) SpawnSingleEyeGhost(eyeRight);
    }

    void SpawnSingleEyeGhost(Light2D source)
    {
        var go = new GameObject("VesperEyeGhost");
        go.transform.position = source.transform.position;

        var ghost = go.AddComponent<Light2D>();
        // Do NOT copy lightType — setting it after AddComponent throws in some Unity versions.
        // The ghost defaults to Point light which is sufficient for the fade effect.
        ghost.color                 = source.color;
        ghost.intensity             = source.intensity;
        ghost.pointLightOuterRadius = source.pointLightOuterRadius;
        ghost.pointLightInnerRadius = source.pointLightInnerRadius;

        _activeEyeGhosts.Add(go);
        StartCoroutine(FadeAndDestroyLight(go, ghost, eyeGhostFadeDuration));
    }

    IEnumerator FadeAndDestroyLight(GameObject go, Light2D light, float duration)
    {
        float start   = light.intensity;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (light != null)
                light.intensity = Mathf.Lerp(start, 0f, elapsed / duration);
            yield return null;
        }
        _activeEyeGhosts.Remove(go);
        if (go != null) Destroy(go);
    }

    void DestroyAllEyeGhosts()
    {
        foreach (var g in _activeEyeGhosts)
            if (g != null) Destroy(g);
        _activeEyeGhosts.Clear();
    }

    /// <summary>
    /// Spike the eye intensity on arrival, then fade back to the current value.
    /// Gives a clear visual "blink in" even in full darkness.
    /// </summary>
    IEnumerator EyeArrivalFlash()
    {
        if (eyeLeft == null && eyeRight == null) yield break;

        float baseIntensity = eyeLeft != null ? eyeLeft.intensity : eyeRight.intensity;
        float peak          = baseIntensity * eyeArrivalFlashMultiplier;

        SetEyeIntensity(peak);

        float elapsed = 0f;
        while (elapsed < eyeArrivalFlashDuration)
        {
            elapsed += Time.deltaTime;
            SetEyeIntensity(Mathf.Lerp(peak, baseIntensity, elapsed / eyeArrivalFlashDuration));
            yield return null;
        }
        SetEyeIntensity(baseIntensity);
    }

    /// <summary>
    /// Spikes the eye light outer radius to phase2ArrivalBurstRadius on arrival,
    /// then fades it back to the pre-burst value. This makes the arrival visible
    /// even in a pitch-black room regardless of where the player's flashlight is aimed.
    /// </summary>
    IEnumerator EyeArrivalRadiusBurst()
    {
        if (eyeLeft == null && eyeRight == null) yield break;

        float baseRadius = eyeLeft != null ? eyeLeft.pointLightOuterRadius
                                           : eyeRight.pointLightOuterRadius;
        float peak = phase2ArrivalBurstRadius;

        SetEyeRadius(peak);

        float elapsed = 0f;
        while (elapsed < phase2ArrivalBurstDuration)
        {
            elapsed += Time.deltaTime;
            SetEyeRadius(Mathf.Lerp(peak, baseRadius, elapsed / phase2ArrivalBurstDuration));
            yield return null;
        }
        SetEyeRadius(baseRadius);
    }

    /// <summary>
    /// Smoothly changes the global Light2D intensity to targetIntensity over duration.
    /// Safe to call when globalLight is null (no-op).
    /// </summary>
    IEnumerator FadeGlobalLight(float targetIntensity, float duration)
    {
        if (globalLight == null) yield break;

        float startIntensity = globalLight.intensity;
        float elapsed        = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            globalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, elapsed / duration);
            yield return null;
        }
        globalLight.intensity = targetIntensity;
    }

    /// <summary>
    /// Shakes both eye transforms in all directions before the boss departs.
    /// X and Y use different frequencies so the motion feels chaotic rather than linear.
    /// Pass omnidirectional=true for death scramble (full 2D), false for the
    /// phase-2 departure shake (horizontal only, more deliberate).
    /// Eyes return to their original local positions when done.
    /// </summary>
    IEnumerator ShakeEyesOnDeparture(float duration, float amplitude, bool omnidirectional = false)
    {
        Vector3 leftOrigin  = eyeLeft  != null ? eyeLeft.transform.localPosition  : Vector3.zero;
        Vector3 rightOrigin = eyeRight != null ? eyeRight.transform.localPosition : Vector3.zero;

        float elapsed = 0f;
        const float freqX = 22f;
        const float freqY = 17f;  // different frequency → non-repeating Lissajous path
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float ox = Mathf.Sin(elapsed * freqX * Mathf.PI * 2f) * amplitude;
            float oy = omnidirectional
                     ? Mathf.Sin(elapsed * freqY * Mathf.PI * 2f) * amplitude
                     : 0f;
            var delta = new Vector3(ox, oy, 0f);
            if (eyeLeft  != null) eyeLeft.transform.localPosition  = leftOrigin  + delta;
            if (eyeRight != null) eyeRight.transform.localPosition = rightOrigin + delta;
            yield return null;
        }

        if (eyeLeft  != null) eyeLeft.transform.localPosition  = leftOrigin;
        if (eyeRight != null) eyeRight.transform.localPosition = rightOrigin;
    }

    IEnumerator FadeEyeColor(Color targetColor, float duration)
    {
        Color startLeft  = eyeLeft  != null ? eyeLeft.color  : Color.white;
        Color startRight = eyeRight != null ? eyeRight.color : Color.white;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (eyeLeft  != null) eyeLeft.color  = Color.Lerp(startLeft,  targetColor, t);
            if (eyeRight != null) eyeRight.color = Color.Lerp(startRight, targetColor, t);
            yield return null;
        }
        if (eyeLeft  != null) eyeLeft.color  = targetColor;
        if (eyeRight != null) eyeRight.color = targetColor;
    }

    /// <summary>FireScatter variant that bypasses the isTeleporting guard — used during Phase 2 transition.</summary>
    void FireScatterForced()
    {
        if (enemyBulletPrefab == null) return;
        int   count     = phase2ScatterBulletCount;
        float angleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float rotation = angleStep * i - 90f;
            Instantiate(enemyBulletPrefab, transform.position, Quaternion.Euler(0f, 0f, rotation));
        }
    }

    float playerDisabledAt = 0f;

    void DisablePlayerInput()
    {
        if (playerTransform == null) return;
        playerDisabledAt = Time.time;
        var rb = playerTransform.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        foreach (var mb in playerTransform.GetComponents<MonoBehaviour>())
        {
            if (mb is PlayerMovement || mb is PlayerShooting || mb is PlayerSlash ||
                mb is PlayerDash     || mb is PlayerLightWave || mb is FlashlightAim)
                mb.enabled = false;
        }
    }

    void EnablePlayerInput()
    {
        if (playerTransform == null) return;
        float disabledDuration = Time.time - playerDisabledAt;
        foreach (var mb in playerTransform.GetComponents<MonoBehaviour>())
        {
            if (mb is PlayerMovement || mb is PlayerShooting || mb is PlayerSlash ||
                mb is PlayerDash     || mb is PlayerLightWave || mb is FlashlightAim)
                mb.enabled = true;
        }
        // Compensate flash cooldown for the time it was frozen while disabled
        playerTransform.GetComponent<PlayerLightWave>()?.AdvanceCooldown(disabledDuration);
    }

    IEnumerator ActivationSequence()
    {
        ApplyBossFightPlayerBuff();

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

    IEnumerator ApplyStartupPlayerBuffNextFrame()
    {
        yield return null;
        ApplyBossFightPlayerBuff();
    }

    void ApplyBossFightPlayerBuff()
    {
        if (playerBuffApplied) return;

        var playerHealth = playerTransform != null ? playerTransform.GetComponent<PlayerHealth>() : null;
        playerHealth?.AddMaxHealth(2);

        PlayerAmmo.Instance?.AddMaxFlashes(2);

        playerBuffApplied = true;
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

        // Phase 2: multiple teleports each followed by a scatter burst.
        // Phase 1: single teleport, no scatter.
        int count = IsPhase2 ? phase2TeleportCount : 1;

        for (int i = 0; i < count; i++)
        {
            // Ghost eyes linger at departure position while boss fades out
            try { SpawnEyeGhosts(); } catch (System.Exception e) { Debug.LogWarning("[VesperAI] SpawnEyeGhosts failed: " + e.Message); }

            // Fade out
            SetAlphaTarget(0f, teleportFadeDuration);
            yield return new WaitForSeconds(teleportFadeDuration);

            // Teleport
            transform.position = FindTeleportPosition();
            yield return new WaitForSeconds(teleportEyeRestoreDelay);

            // Snap visible + arrival flash
            ApplyAlpha(bodyMaxAlpha);
            SetAlphaTarget(bodyMaxAlpha, 0.01f);
            StartCoroutine(EyeArrivalFlash());

            // Phase 2 fires after every teleport
            if (IsPhase2)
                FireScatterForced();

            // Between bursts: use phase2TeleportInterval.
            // After the last burst: use teleportArrivalDuration so the body stays
            // visible at bodyMaxAlpha before Update fades it to bodyMinAlpha.
            float waitTime = (i < count - 1) ? phase2TeleportInterval : teleportArrivalDuration;
            yield return new WaitForSeconds(waitTime);
        }

        // Reset per-cycle counters
        kHitCount             = 0;
        dashHitCount          = 0;
        slashDamageThisCycle  = 0f;
        meleeProximityTimer   = 0f;
        slashStaggerAvailable = true;
        inSlashStagger        = false;
        vulnerableTimer       = 0f;

        isTeleporting = false;
        // Body is still at bodyMaxAlpha. Update will fade it to bodyMinAlpha
        // if the player isn't illuminating the boss.
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

    /// <summary>
    /// Returns 3 world positions for the Phase 2 cinematic teleport sequence.
    /// Picks a far destination (opposite side of the room from the player), then
    /// builds two intermediate hops that zigzag left and right of the path,
    /// giving the classic "flicker side-to-side before landing" feel.
    /// </summary>
    Vector2[] FindPhase2TransitionPositions()
    {
        Vector2 start     = transform.position;
        Vector2 playerPos = playerTransform != null ? (Vector2)playerTransform.position : roomCenter;

        // Destination: push toward the room edge farthest from the player
        Vector2 awayDir = (roomCenter - playerPos).normalized;
        if (awayDir == Vector2.zero) awayDir = Vector2.up;
        Vector2 destination = roomCenter + awayDir * new Vector2(roomHalfSize.x, roomHalfSize.y) * 0.78f;
        destination = ClampToRoom(destination);

        // Perpendicular axis for the zigzag
        Vector2 path = destination - start;
        Vector2 perp = path.sqrMagnitude > 0.01f
                     ? new Vector2(-path.y, path.x).normalized
                     : Vector2.right;

        // Hop 1: ~35% along path, offset LEFT
        // Hop 2: ~70% along path, offset RIGHT
        // Hop 3: final destination (no side offset)
        Vector2 pos1 = ClampToRoom(Vector2.Lerp(start, destination, 0.35f) + perp *  phase2TransitionZigzagOffset);
        Vector2 pos2 = ClampToRoom(Vector2.Lerp(start, destination, 0.70f) - perp *  phase2TransitionZigzagOffset);
        Vector2 pos3 = destination;

        return new Vector2[] { pos1, pos2, pos3 };
    }

    Vector2 ClampToRoom(Vector2 pos)
    {
        pos.x = Mathf.Clamp(pos.x, roomCenter.x - roomHalfSize.x * 0.8f, roomCenter.x + roomHalfSize.x * 0.8f);
        pos.y = Mathf.Clamp(pos.y, roomCenter.y - roomHalfSize.y * 0.8f, roomCenter.y + roomHalfSize.y * 0.8f);
        return pos;
    }

    IEnumerator DeathSequence()
    {
        if (state == BossState.Dead) yield break;
        state         = BossState.Dead;
        isTeleporting = true;

        // StopAllCoroutines() may have interrupted Phase2TransitionSequence mid-way,
        // leaving the player frozen and global light at a non-zero value — reset both.
        EnablePlayerInput();
        if (globalLight != null) globalLight.intensity = 0f;

        // Destroy ghost objects from any interrupted teleport coroutines so we don't
        // end up with stale frozen eye ghosts at the kill spot alongside new ones.
        DestroyAllEyeGhosts();

        if (healthBar != null) healthBar.Hide();

        // Freeze player and lock camera onto boss for the entire cinematic
        DisablePlayerInput();
        bossIntroCam?.FocusOnBoss(transform.position);

        // Record baseline eye state — will be degraded across each hop
        float eyeIntensity = eyeLeft != null ? eyeLeft.intensity             : 1f;
        float eyeRadius    = eyeLeft != null ? eyeLeft.pointLightOuterRadius : 1f;
        Color eyeColor     = eyeLeft != null ? eyeLeft.color                 : Color.red;
        Color greyTarget   = new Color(0.35f, 0.35f, 0.35f, 1f);

        // ── Frantic death scramble ────────────────────────────────────────────
        // Rapid random teleports with no anticipation shake — panicked, erratic.
        // All hops use random dark positions; only the very last settles at the
        // designated death spot for the final candle-flicker sequence.
        for (int i = 0; i < deathTeleportCount; i++)
        {
            float hopFrac = (float)(i + 1) / deathTeleportCount;  // 0→1 across hops
            bool  lastHop = (i == deathTeleportCount - 1);

            // Omnidirectional shake — gets longer and weaker as boss slows down and dies
            float shakeAmp = phase2DepartureShakeAmplitude * (1.4f - hopFrac * 0.8f);
            float shakeDur = phase2DepartureShakeDuration  * Mathf.Lerp(0.4f, 1.0f, hopFrac);
            yield return StartCoroutine(ShakeEyesOnDeparture(shakeDur, shakeAmp, omnidirectional: true));

            // Ghost eyes linger at departure; body snaps out immediately
            SpawnEyeGhosts();
            ApplyAlpha(0f);
            SetAlphaTarget(0f, 0.01f);
            yield return new WaitForSeconds(deathHopFadeDuration);

            // All hops are random — last one uses the designated death position
            transform.position = lastHop ? FindDeathPosition() : FindTeleportPosition();
            bossIntroCam?.SnapToPosition(transform.position);

            yield return new WaitForSeconds(deathHopEyeRestoreDelay);

            // Degrade eyes: dimmer and greyer each hop
            eyeIntensity *= deathEyeIntensityDecayPerHop;
            eyeColor      = Color.Lerp(eyeColor, greyTarget, hopFrac * 0.6f);
            SetEyeIntensity(eyeIntensity);
            if (eyeLeft  != null) eyeLeft.color  = eyeColor;
            if (eyeRight != null) eyeRight.color = eyeColor;

            // Body flashes back at decaying alpha
            float arrivalAlpha = bodyMaxAlpha * Mathf.Lerp(1f, 0.4f, hopFrac);
            ApplyAlpha(arrivalAlpha);
            SetAlphaTarget(arrivalAlpha, 0.01f);

            // Visible pause grows each hop — starts frantic, slows into dying exhaustion
            float pause = lastHop ? 0f : deathHopPauseDuration * Mathf.Lerp(0.4f, 2.0f, hopFrac);
            if (pause > 0f)
                yield return new WaitForSeconds(pause);
        }

        // ── Final dying flicker at last position ──────────────────────────────
        yield return new WaitForSeconds(0.15f);

        // Candle flicker drives eye intensity to 0; body fades on the "last gasp" keyframe
        StartCoroutine(DeathBodyFadeDelayed(deathBodyFadeDuration));
        yield return StartCoroutine(DeathCandleFlicker(eyeIntensity, eyeRadius));

        // Brief dark pause, then camera pans back to player
        yield return new WaitForSeconds(deathHoldDuration);

        if (bossIntroCam != null && playerTransform != null)
            yield return StartCoroutine(bossIntroCam.PanBackToPlayer(playerTransform));

        // Player can move again while waiting for the portal to spawn
        EnablePlayerInput();

        // Short breather, then spawn the room-clear portal (progressive mode)
        // or trigger the victory screen (standalone VesperScene)
        if (deathPortalDelay > 0f)
            yield return new WaitForSeconds(deathPortalDelay);

        GameManager.Instance?.BossDefeated();
    }

    /// <summary>
    /// Picks an open position for the death cinematic: away from the player,
    /// centred in the room rather than pushed to the wall.
    /// </summary>
    Vector2 FindDeathPosition()
    {
        Vector2 playerPos = playerTransform != null ? (Vector2)playerTransform.position : roomCenter;
        Vector2 awayDir   = (roomCenter - playerPos).normalized;
        if (awayDir == Vector2.zero) awayDir = Vector2.up;

        // Push from centre in the away direction, clamped well inside room edges
        Vector2 pos = roomCenter + awayDir * new Vector2(
            roomHalfSize.x * deathRetreatRoomFraction,
            roomHalfSize.y * deathRetreatRoomFraction);

        return ClampToRoom(pos);
    }


    /// <summary>
    /// "Dying candle" eye flicker: dims → brief flare (fighting back) → nearly out →
    /// one last gasp → radius collapses → dark.
    /// Total approx duration: 0.38 + 0.13 + 0.52 + 0.09 + 0.72 ≈ 1.84 s.
    /// </summary>
    IEnumerator DeathCandleFlicker(float baseIntensity, float baseRadius)
    {
        // Each entry: (intensity multiplier, radius multiplier, duration in seconds)
        // Read as: lerp FROM current TO (base × multiplier) over duration.
        float[] intensityMult = { 0.28f, 0.68f, 0.07f, 0.42f, 0.0f };
        float[] radiusMult    = { 1.00f, 1.25f, 0.45f, 0.75f, 0.0f };
        float[] durations     = { 0.38f, 0.13f, 0.52f, 0.09f, 0.72f };

        // Body starts fading at the "last gasp" keyframe (index 3)
        float bodyFadeDelay = durations[0] + durations[1] + durations[2];

        for (int k = 0; k < durations.Length; k++)
        {
            float fromIntensity = eyeLeft != null ? eyeLeft.intensity             : 0f;
            float fromRadius    = eyeLeft != null ? eyeLeft.pointLightOuterRadius : 1f;
            float toIntensity   = baseIntensity * intensityMult[k];
            float toRadius      = baseRadius    * radiusMult[k];
            float dur           = durations[k];

            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t  = elapsed / dur;
                SetEyeIntensity(Mathf.Lerp(fromIntensity, toIntensity, t));
                SetEyeRadius   (Mathf.Lerp(fromRadius,    toRadius,    t));
                yield return null;
            }
        }

        SetEyeIntensity(0f);
        SetEyeRadius(0f);
    }

    IEnumerator DeathBodyFadeDelayed(float fadeDuration)
    {
        // Wait until the candle is on its "last gasp" before body starts vanishing
        float delay = 0.38f + 0.13f + 0.52f;  // matches bodyFadeDelay in DeathCandleFlicker
        yield return new WaitForSeconds(delay);

        float startAlpha = currentAlpha;
        float elapsed    = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            ApplyAlpha(Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration));
            yield return null;
        }
        ApplyAlpha(0f);
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
    /// Called by DungeonManager after spawning Vesper in progressive mode to inject
    /// the arena bounds so teleport sampling and death positioning work correctly.
    /// </summary>
    public void SetRoomBounds(Vector2 center, Vector2 halfSize)
    {
        roomCenter   = center;
        roomHalfSize = halfSize;
    }

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
