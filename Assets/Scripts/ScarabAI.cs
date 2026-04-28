using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;
using System.Collections;

/// <summary>
/// Boss AI for Scarab (Boss 2).
///
/// COLLIDER SETUP:
///   Scarab uses trigger colliders only (isTrigger = true) — no solid body.
///   Bullet.cs returns early for trigger colliders, so ScarabAI.OnTriggerEnter2D
///   handles bullet hits and destroys bullets manually.
///   Slash hits are detected via the SlashBulletDeflector component on slash light objects.
///
/// FACING / ARMOR:
///   facingDir is updated by movement (chase) and charge direction.
///   Front armor (dot(facingDir, dir_to_player) > 0):
///     Slash → 0 damage, grey "0", player knocked back, Scarab flashes white.
///     Bullet → 0 damage, grey "0", bullet destroyed.
///   Back weak point ACTIVE (afterlag):  slash/bullet → 2 damage.
///   Back weak point INACTIVE:           slash/bullet → 1 damage.
///
/// PLAYER INVINCIBILITY:
///   Managed from boss side (playerInvincibleTimer). Only blocks Scarab-dealt
///   damage; other sources still go through PlayerHealth's own iFrame system.
/// </summary>
public class ScarabAI : MonoBehaviour
{
    // ── Inspector Parameters ─────────────────────────────────────────────────

    [Header("Stats")]
    [SerializeField] int maxHealth        = 30;
    [SerializeField] int phase2HPThreshold = 15;

    [Header("Chase (Phase 1)")]
    [SerializeField] float chaseSpeed    = 3f;
    [SerializeField] int   contactDamage        = 1;

    [Header("Normal Charge (Phase 1)")]
    [SerializeField] float chargeWarningWidth      = 1.0f;  // fallback if renderers unavailable
    [SerializeField] float chargeWarningWidthScale = 0.7f;  // fraction of wingspan to use
    [SerializeField] float chargeTurnDuration = 1.8f;   // how long boss turns back to player after afterlag
    [SerializeField] float chargeWindup   = 0.8f;
    [SerializeField] float chargeHoldAfterWindup = 0.2f;
    [SerializeField] float chargeSpeed              = 22f;
    [SerializeField] float chargeSpeedDistanceFactor = 0.5f; // extra speed per unit of distance
    [SerializeField] float chargeDistance = 20f;
    [SerializeField] float chargeCD       = 4f;
    [SerializeField] float normalAfterlag = 3f;
    [SerializeField] int   chargeDamage   = 2;
    [SerializeField] float chargeHitRadius = 0.9f;

    [Header("Phase 2 Transition")]
    [SerializeField] float p2ShockwaveRadius      = 34f;
    [SerializeField] float p2ShockwaveDuration    = 0.5f;
    [SerializeField] float p2KnockbackDistance    = 4f;
    [SerializeField] float p2KnockbackTime        = 0.2f;
    [SerializeField] float p2LandingDelay         = 1.5f;

    [Header("Phase 2")]
    [SerializeField] float p2FlightHeight          = 4f;
    [SerializeField] float p2DiveSpeed             = 30f;
    [SerializeField] float diveTimeBuffer          = 0.3f;
    [SerializeField] float phase2AfterlagDuration  = 3f;
    [SerializeField] float phase2WallStunDuration  = 3f;
    [SerializeField] float phase2WallShake         = 0.4f;
    [SerializeField] float phase2ChargeCD          = 3f;
    [SerializeField] float jumpWindup              = 0.5f;
    [SerializeField] float jumpLandRadius          = 2.5f;  // max random offset from player
    [SerializeField] float empoweredChargeSpeed    = 16f;
    [SerializeField] float empoweredChargeDistance = 12f;
    [SerializeField] int   empoweredChargeDamage   = 3;
    [SerializeField] float playerStunDuration      = 2f;
    [SerializeField] float empoweredChargeWallMax  = 50f;   // safety cap for wall-crash charge

    [Header("Player Protection")]
    [SerializeField] float playerInvincibleDuration = 2f;
    [SerializeField] float playerBlinkInterval      = 0.15f;

    [Header("Lights")]
    [SerializeField] float weakPointIntensity    = 2f;
    [SerializeField] float headFlashIntensity    = 3f;

    [Header("Screen Shake")]
    [SerializeField] float screenShakeNormal    = 0.3f;
    [SerializeField] float screenShakeWallHit   = 0.4f;

    [Header("Armor Knockback")]
    [SerializeField] float armorKnockbackDistance = 3f;
    [SerializeField] float armorKnockbackTime     = 0.12f;
    [SerializeField] float armorShakeStrength     = 0.25f;

    [Header("Charge Knockback")]
    [SerializeField] float chargeKnockbackDistance = 6f;
    [SerializeField] float chargeKnockbackTime     = 0.18f;
    [SerializeField] float chargeKnockbackShake    = 0.45f;


    [Header("Wall Detection")]
    [SerializeField] LayerMask wallLayerMask;
    [SerializeField] float     wallCheckRadius = 0.4f;

    [Header("Fly-In Entrance")]
    [SerializeField] float sweepScaleMultiplier = 4f;   // boss scale during cinematic sweep
    [SerializeField] float sweepDuration        = 2.0f; // time to cross the screen
    [SerializeField] float sweepHeightOffset    = 2.5f; // how far above player Y the silhouette flies
    [SerializeField] float sweepPauseDuration   = 1.2f; // pause between sweep end and descent start
    [SerializeField] float flyInDescentHeight   = 8f;   // how far above finalPos descent begins
    [SerializeField] float flyInDuration        = 3.8f; // descent duration

    [Header("Wings")]
    [SerializeField] SpriteRenderer flapWingLeft;   // semi-transparent flap wings (new triangles)
    [SerializeField] SpriteRenderer flapWingRight;
    [SerializeField] float wingChaseFlapSpeed  = 4f;   // lazy idle flap while chasing
    [SerializeField] float wingChargeFlapSpeed = 14f;  // rapid burst during windup/charge
    [SerializeField] float wingMaxAlpha    = 0.72f; // fully extended alpha
    [SerializeField] float wingMinAlpha    = 0.05f; // fully folded alpha

    [Header("References")]
    [SerializeField] Light2D      headLight;
    [SerializeField] Light2D      backLight;
    [SerializeField] BossHealthBar healthBar;
    [SerializeField] BossIntroCam  bossIntroCam;

    [Header("Death")]
    [SerializeField] float deathPortalDelay = 1.5f;

    [Header("Room Bounds")]
    [SerializeField] Vector2 roomCenter   = Vector2.zero;
    [SerializeField] Vector2 roomHalfSize = new Vector2(17f, 17f);

    // ── State ────────────────────────────────────────────────────────────────

    enum ScarabState { Dormant, InBattle, Dead }
    ScarabState state = ScarabState.Dormant;

    // Slash hits from body + child parts are collected each frame and resolved once in LateUpdate.
    // pendingSlashArcID / lastResolvedSlashArcID prevent the same slash arc from being resolved
    // twice when boss rotation causes a child collider to leave and re-enter the arc trigger.
    enum SlashResult { None, Armor, Damage }
    SlashResult pendingSlash           = SlashResult.None;
    int         pendingSlashFrame      = -1;
    int         pendingSlashDmg        = 0;
    int         pendingSlashArcID      = -1;
    int         lastResolvedSlashArcID = -1;

    int lastProcessedBulletID = -1; // dedup: same bullet can hit body + child in one physics step

    int  currentHealth;
    bool IsPhase2 => currentHealth <= phase2HPThreshold && currentHealth > 0;

    Vector2 facingDir = Vector2.down;

    bool  backWeakPointActive    = false;
    bool  isAttacking            = false;   // true during charge + afterlag; blocks chase
    bool  isCharging             = false;   // true only while ExecuteCharge is moving the boss
    bool  playerIsStunned        = false;
    bool  playerDashedDuringStun = false;
    bool  phase2Entered          = false;
    bool  phase2TransitionActive = false;
    bool  isAirborne             = false;
    bool  knockbackActive        = false;
    bool  isWallStunned          = false;
    int   wallStunSlashHits      = 0;
    int   wallStunDamageTotal    = 0;
    float playerInvincibleTimer  = 0f;

    Rigidbody2D    rb;
    SpriteRenderer sr;
    Transform      playerTransform;
    PlayerHealth   playerHealth;
    Rigidbody2D    playerRb;
    SpriteRenderer playerSR;
    PlayerMovement playerMovement;

    bool      introScheduled = false;
    Coroutine blinkRoutine;
    Coroutine wingCoroutine;
    Coroutine armorKnockbackRoutine;
    bool      wingsActive    = false;   // true while wings should be flapping

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // Body = 10, flapWings = 11, static wings = 12, head = 13
        foreach (var r in GetComponentsInChildren<SpriteRenderer>())
            r.sortingOrder = 10;

        if (flapWingLeft  != null) flapWingLeft.sortingOrder  = 11;
        if (flapWingRight != null) flapWingRight.sortingOrder = 11;

        foreach (var fwd in GetComponentsInChildren<ScarabHitForwarder>())
        {
            var partSR = fwd.GetComponent<SpriteRenderer>();
            if (partSR == null) continue;
            partSR.sortingOrder = fwd.IsHead ? 13 : 12;
        }

        if (headLight != null) headLight.intensity = 0f;
        if (backLight != null) backLight.intensity = 0f;
    }

    void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerHealth    = playerObj.GetComponent<PlayerHealth>();
            playerRb        = playerObj.GetComponent<Rigidbody2D>();
            playerSR        = playerObj.GetComponent<SpriteRenderer>();
            playerMovement  = playerObj.GetComponent<PlayerMovement>();
            if (playerMovement != null)
                playerMovement.OnDashStart += OnPlayerDash;
        }
    }

    void OnDestroy()
    {
        if (playerMovement != null)
            playerMovement.OnDashStart -= OnPlayerDash;
    }

    void OnPlayerDash()
    {
        if (playerIsStunned)
            playerDashedDuringStun = true;
    }

    void Update()
    {
        if (playerInvincibleTimer > 0f) playerInvincibleTimer -= Time.deltaTime;

        // Rotate sprite to match facing direction (sprite default = up)
        if (facingDir != Vector2.zero)
        {
            float angle = Mathf.Atan2(facingDir.y, facingDir.x) * Mathf.Rad2Deg + 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    // ── Wing Helpers ─────────────────────────────────────────────────────────

    void SetWingAlpha(float a)
    {
        if (flapWingLeft  != null) { var c = flapWingLeft.color;  c.a = a; flapWingLeft.color  = c; }
        if (flapWingRight != null) { var c = flapWingRight.color; c.a = a; flapWingRight.color = c; }
    }

    void StartWingFlap(float speed)
    {
        currentWingFlapSpeed = speed;
        wingsActive = true;
        if (wingCoroutine != null) StopCoroutine(wingCoroutine);
        wingCoroutine = StartCoroutine(WingFlapLoop());
    }

    void StopWingFlap() => wingsActive = false;

    float currentWingFlapSpeed;

    IEnumerator WingFlapLoop()
    {
        float phase = 0f;
        while (wingsActive)
        {
            phase += Time.deltaTime * currentWingFlapSpeed * Mathf.PI * 2f;
            // Sine mapped to [0,1], then remapped to [min,max]
            float t     = (Mathf.Sin(phase) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(wingMinAlpha, wingMaxAlpha, t);
            SetWingAlpha(alpha);
            yield return null;
        }
        // Smoothly fold wings away when flapping stops
        yield return StartCoroutine(FadeWingAlpha(GetWingAlpha(), wingMinAlpha, 0.18f));
    }

    float GetWingAlpha() => flapWingLeft != null ? flapWingLeft.color.a : 0f;

    IEnumerator FadeWingAlpha(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetWingAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetWingAlpha(to);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Called by ScarabArenaTrigger — ensures only the first trigger fires the intro.</summary>
    public bool TryScheduleIntro()
    {
        if (introScheduled) return false;
        introScheduled = true;
        return true;
    }

    /// <summary>
    /// Full fly-in entrance. Yields until complete (including camera restore).
    /// ScarabArenaTrigger yields on this coroutine directly.
    /// </summary>
    public IEnumerator RunFlyInEntrance()
    {
        Vector2 finalPos   = transform.position;
        Vector3 finalScale = transform.localScale;
        Vector2 playerPos  = playerTransform != null ? (Vector2)playerTransform.position : finalPos;

        Camera       cam         = Camera.main;
        CameraFollow camFollow   = cam != null ? cam.GetComponent<CameraFollow>() : null;
        Transform    savedTarget = camFollow != null ? camFollow.target : null;

        var allRenderers = GetComponentsInChildren<SpriteRenderer>();
        var savedColors  = new Color[allRenderers.Length];
        for (int i = 0; i < allRenderers.Length; i++)
            savedColors[i] = allRenderers[i].color;
        Color savedFlapL = flapWingLeft  != null ? flapWingLeft.color  : Color.clear;
        Color savedFlapR = flapWingRight != null ? flapWingRight.color : Color.clear;

        // ── Phase 1: Giant black silhouette sweeps left → right ──────────────
        // Boss painted solid black at sweepScaleMultiplier × finalScale.
        // Sweeps at player height; the giant silhouette swamps the entire camera
        // lens as it passes — cinematic creature flyby. Camera stays on player.
        float camHalfW    = cam != null ? cam.orthographicSize * cam.aspect + 5f : 15f;
        float snapCamX    = cam != null ? cam.transform.position.x : playerPos.x;
        float bossWidth   = GetWarnWidth() * sweepScaleMultiplier;
        float sweepStartX = snapCamX - camHalfW;
        float sweepEndX   = snapCamX + camHalfW + bossWidth;

        transform.localScale = finalScale * sweepScaleMultiplier;
        transform.position   = new Vector3(sweepStartX, playerPos.y + sweepHeightOffset, 0f);
        facingDir            = Vector2.right;

        // Paint all renderers black — silhouette look
        for (int i = 0; i < allRenderers.Length; i++)
            allRenderers[i].color = Color.black;

        StartWingFlap(wingChargeFlapSpeed);

        float elapsed = 0f;
        while (elapsed < sweepDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / sweepDuration);
            transform.position = new Vector3(Mathf.Lerp(sweepStartX, sweepEndX, t), playerPos.y + sweepHeightOffset, 0f);
            yield return null;
        }

        StopWingFlap();

        // Hide boss while repositioning — zero out sprites only, lights untouched
        foreach (var r in allRenderers) { var c = r.color; c.a = 0f; r.color = c; }
        SetWingAlpha(0f);

        yield return new WaitForSeconds(sweepPauseDuration);

        // ── Phase 2: Descent from above finalPos ──────────────────────────────
        Vector2 descentStart = ClampToRoom(new Vector2(finalPos.x, finalPos.y + flyInDescentHeight));
        transform.position   = descentStart;
        transform.localScale = finalScale * sweepScaleMultiplier;
        facingDir            = Vector2.down;

        // Start: black and fully transparent
        for (int i = 0; i < allRenderers.Length; i++)
            allRenderers[i].color = new Color(0f, 0f, 0f, 0f);

        if (camFollow != null) camFollow.target = transform;

        // Manual flap so we control RGB + alpha together during the color restore
        if (wingCoroutine != null) { StopCoroutine(wingCoroutine); wingCoroutine = null; }
        float descentFlapPhase = 0f;

        elapsed = 0f;
        while (elapsed < flyInDuration)
        {
            elapsed += Time.deltaTime;
            float t      = Mathf.Clamp01(elapsed / flyInDuration);
            float alphaT = Mathf.Clamp01(t / 0.4f);           // 0→1 over first 40%
            float colorT = Mathf.Clamp01((t - 0.3f) / 0.7f);  // 0→1 from 30% to 100%
            descentFlapPhase += Time.deltaTime * wingChaseFlapSpeed * Mathf.PI * 2f;

            transform.position   = Vector2.Lerp(descentStart, (Vector2)finalPos, t);
            transform.localScale = Vector3.Lerp(finalScale * sweepScaleMultiplier, finalScale, t);

            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] == flapWingLeft || allRenderers[i] == flapWingRight) continue;
                Color orig = savedColors[i];
                allRenderers[i].color = new Color(
                    Mathf.Lerp(0f, orig.r, colorT),
                    Mathf.Lerp(0f, orig.g, colorT),
                    Mathf.Lerp(0f, orig.b, colorT),
                    orig.a * alphaT);
            }

            // FlapWings: flap alpha * alphaT, RGB black → savedFlapColor
            float flapSine = (Mathf.Sin(descentFlapPhase) + 1f) * 0.5f;
            float flapA    = Mathf.Lerp(wingMinAlpha, wingMaxAlpha, flapSine) * alphaT;
            if (flapWingLeft != null)
                flapWingLeft.color = new Color(
                    Mathf.Lerp(0f, savedFlapL.r, colorT),
                    Mathf.Lerp(0f, savedFlapL.g, colorT),
                    Mathf.Lerp(0f, savedFlapL.b, colorT), flapA);
            if (flapWingRight != null)
                flapWingRight.color = new Color(
                    Mathf.Lerp(0f, savedFlapR.r, colorT),
                    Mathf.Lerp(0f, savedFlapR.g, colorT),
                    Mathf.Lerp(0f, savedFlapR.b, colorT), flapA);

            yield return null;
        }

        transform.position   = finalPos;
        transform.localScale = finalScale;
        for (int i = 0; i < allRenderers.Length; i++) allRenderers[i].color = savedColors[i];
        if (flapWingLeft  != null) flapWingLeft.color  = new Color(savedFlapL.r, savedFlapL.g, savedFlapL.b, wingMaxAlpha);
        if (flapWingRight != null) flapWingRight.color = new Color(savedFlapR.r, savedFlapR.g, savedFlapR.b, wingMaxAlpha);

        yield return new WaitForSeconds(0.15f);

        // ── Slam ──────────────────────────────────────────────────────────────
        CameraShake.Instance?.Shake(0.7f, 0.4f);
        yield return StartCoroutine(FadeWingAlpha(GetWingAlpha(), wingMinAlpha, 0.1f));
        yield return new WaitForSeconds(0.4f);

        // ── Two wing flaps after landing ──────────────────────────────────────
        float flapPhase    = 0f;
        float flapDuration = 2f / 3f;
        while (flapPhase < flapDuration)
        {
            flapPhase += Time.deltaTime;
            float t = flapPhase / flapDuration * 2f * Mathf.PI * 2f - Mathf.PI * 0.5f;
            SetWingAlpha(Mathf.Lerp(wingMinAlpha, wingMaxAlpha, (Mathf.Sin(t) + 1f) * 0.5f));
            yield return null;
        }
        SetWingAlpha(wingMinAlpha);
        yield return new WaitForSeconds(0.2f);

        // ── Camera pans back to player — 2s ───────────────────────────────────
        if (camFollow != null) camFollow.enabled = false;
        if (cam != null && savedTarget != null)
        {
            Vector3 camFrom  = cam.transform.position;
            Vector3 camTo    = new Vector3(savedTarget.position.x, savedTarget.position.y, -10f);
            float panElapsed = 0f;
            while (panElapsed < 2.0f)
            {
                panElapsed += Time.deltaTime;
                cam.transform.position = Vector3.Lerp(camFrom, camTo,
                    Mathf.SmoothStep(0f, 1f, panElapsed / 2.0f));
                yield return null;
            }
        }
        if (camFollow != null) { camFollow.target = savedTarget; camFollow.enabled = true; }
    }

    /// <summary>Called by ScarabArenaTrigger after the intro camera returns to the player.</summary>
    public void StartBattle()
    {
        if (state != ScarabState.Dormant) return;
        state = ScarabState.InBattle;

        if (healthBar != null)
        {
            healthBar.Initialize(maxHealth);
            healthBar.Show();
        }

        StartCoroutine(BattleLoop());
    }

    // ── Battle Loop ──────────────────────────────────────────────────────────

    IEnumerator BattleLoop(bool immediate = false)
    {
        float attackTimer = immediate ? 0f : (phase2Entered ? phase2ChargeCD : chargeCD);

        while (state == ScarabState.InBattle)
        {
            // Handle phase 2 entry — reset attack timer once
            if (IsPhase2 && !phase2Entered)
            {
                phase2Entered = true;
                attackTimer   = phase2ChargeCD;
            }

            if (!isAttacking && playerTransform != null)
            {
                if (!wingsActive) StartWingFlap(wingChaseFlapSpeed);

                if (!IsPhase2)
                {
                    // Chase player
                    Vector2 dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                    if (dir != Vector2.zero)
                    {
                        facingDir = dir;
                        if (rb != null) rb.linearVelocity = dir * chaseSpeed;
                    }
                }
                else
                {
                    // Stop chasing in Phase 2
                    if (rb != null) rb.linearVelocity = Vector2.zero;
                }

                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    StopWingFlap();
                    isAttacking = true;
                    if (rb != null) rb.linearVelocity = Vector2.zero;

                    if (!IsPhase2)
                    {
                        yield return StartCoroutine(NormalChargeSequence());
                        attackTimer = chargeCD;
                    }
                    else
                    {
                        yield return StartCoroutine(JumpChargeSequence());
                        yield return StartCoroutine(P2TakeoffSequence());
                        attackTimer = 0f;
                    }

                    isAttacking = false;
                }
            }

            yield return null;
        }
    }

    // ── Phase 1: Normal Charge ───────────────────────────────────────────────

    IEnumerator NormalChargeSequence()
    {
        if (playerTransform == null) yield break;

        // Windup: direction + distance track player continuously; lock on windup end
        StartWingFlap(wingChargeFlapSpeed);
        Vector2    chargeDir = facingDir;
        float      dynDist   = chargeDistance;
        GameObject warn      = CreateWarningRect(chargeDir, dynDist, new Color(1f, 0.1f, 0.1f));
        float      elapsed   = 0f;
        while (elapsed < chargeWindup)
        {
            elapsed += Time.deltaTime;
            Vector2 toPlayer = (Vector2)playerTransform.position - (Vector2)transform.position;
            chargeDir = toPlayer.magnitude > 0.01f ? toPlayer.normalized : chargeDir;
            dynDist   = Mathf.Max(toPlayer.magnitude, 2f);
            facingDir = chargeDir;
            UpdateWarningRect(warn, transform.position, chargeDir, dynDist, elapsed / chargeWindup);
            yield return null;
        }
        // Hold at full for 0.5s so player can react — direction locked
        float holdElapsed = 0f;
        while (holdElapsed < chargeHoldAfterWindup)
        {
            holdElapsed += Time.deltaTime;
            UpdateWarningRect(warn, transform.position, chargeDir, dynDist, 1f);
            yield return null;
        }
        DestroyWarningRect(warn);
        float dynSpeed = chargeSpeed + dynDist * chargeSpeedDistanceFactor;
        yield return StartCoroutine(ExecuteCharge(chargeDir, dynSpeed, dynDist,
            chargeDamage));
        StopWingFlap();

        yield return StartCoroutine(AfterlagSequence(normalAfterlag, chargeDir));
        yield return StartCoroutine(SmoothTurn(chargeTurnDuration));
    }

    // ── Phase 2: Takeoff + Dive ──────────────────────────────────────────────

    IEnumerator JumpChargeSequence()
    {
        if (playerTransform == null) yield break;

        // ── Liftoff windup ────────────────────────────────────────────────
        isAirborne = true;
        StartWingFlap(wingChargeFlapSpeed);

        Vector3 baseScale  = transform.localScale;
        Vector3 apexScale  = baseScale * 1.2f;
        Vector2 groundPos  = transform.position;
        Vector2 apexPos    = ClampToRoom(groundPos + Vector2.up * p2FlightHeight);



        float liftElapsed = 0f;
        while (liftElapsed < jumpWindup)
        {
            liftElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, liftElapsed / jumpWindup);
            transform.position   = Vector2.Lerp(groundPos, apexPos, t);
            transform.localScale = Vector3.Lerp(baseScale, apexScale, t);
            yield return null;
        }
        transform.position   = apexPos;
        transform.localScale = apexScale;

        yield return new WaitForSeconds(0.15f);

        // ── Dive at player ────────────────────────────────────────────────
        Vector2 playerPos  = playerTransform != null ? (Vector2)playerTransform.position : apexPos;
        float   landOff    = Random.Range(0f, jumpLandRadius * 0.4f);
        float   landAngle  = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 diveTarget = ClampToRoom(playerPos + new Vector2(Mathf.Cos(landAngle), Mathf.Sin(landAngle)) * landOff);

        facingDir = (diveTarget - apexPos).magnitude > 0.01f
            ? (diveTarget - apexPos).normalized : Vector2.down;

        float totalDist        = Vector2.Distance(apexPos, diveTarget);
        float estimatedDiveDur = totalDist / p2DiveSpeed + diveTimeBuffer;

        StartCoroutine(P2LandingWarning(diveTarget, GetWarnWidth() * 0.5f, estimatedDiveDur));

        float diveElapsed = 0f;
        while (diveElapsed < estimatedDiveDur)
        {
            diveElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, diveElapsed / estimatedDiveDur);
            transform.position   = Vector2.Lerp(apexPos, diveTarget, t);
            transform.localScale = Vector3.Lerp(apexScale, baseScale, t);
            yield return null;
        }

        transform.position   = diveTarget;
        transform.localScale = baseScale;
        isAirborne           = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;


        // ── Impact ────────────────────────────────────────────────────────
        CameraShake.Instance?.Shake(screenShakeNormal, screenShakeNormal);
        StartCoroutine(P2Shockwave(diveTarget, 4f, 0.3f));

        float jumpCircleRadius = GetWarnWidth() * 0.5f;
        bool circleHit = playerTransform != null &&
            Vector2.Distance(diveTarget, playerTransform.position) <= jumpCircleRadius;

        if (circleHit)
        {
            DealDamageToPlayer(2);
            Vector2 knockDir = ((Vector2)playerTransform.position - diveTarget).normalized;
            if (knockDir == Vector2.zero) knockDir = Vector2.up;
            yield return StartCoroutine(ChargeKnockbackSequence(knockDir));
        }

        yield return StartCoroutine(DiveFollowUpCharge());
    }

    IEnumerator P2TakeoffSequence()
    {
        StartWingFlap(wingChargeFlapSpeed);

        Vector2 startDir  = facingDir;
        Vector2 targetDir = playerTransform != null
            ? ((Vector2)playerTransform.position - (Vector2)transform.position).normalized
            : facingDir;
        if (targetDir == Vector2.zero) targetDir = facingDir;

        float elapsed            = 0f;
        const float turnDuration = 1.8f;

        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            facingDir = Vector2.Lerp(startDir, targetDir,
                Mathf.SmoothStep(0f, 1f, elapsed / turnDuration)).normalized;
            yield return null;
        }

        facingDir = targetDir;
        StopWingFlap();
    }

    IEnumerator DiveFollowUpCharge()
    {
        if (playerTransform == null) yield break;

        Vector2    chargeDir = facingDir;
        StartWingFlap(wingChargeFlapSpeed);
        float wallDist = DistanceToWall(transform.position, chargeDir);
        GameObject warn = CreateWarningRect(chargeDir, wallDist, new Color(1f, 0.1f, 0.1f));
        float elapsed = 0f;
        while (elapsed < chargeWindup)
        {
            elapsed += Time.deltaTime;
            Vector2 toPlayer = (Vector2)playerTransform.position - (Vector2)transform.position;
            chargeDir = toPlayer.magnitude > 0.01f ? toPlayer.normalized : chargeDir;
            facingDir = chargeDir;
            wallDist  = DistanceToWall(transform.position, chargeDir);
            UpdateWarningRect(warn, transform.position, chargeDir, wallDist, elapsed / chargeWindup);
            yield return null;
        }
        float holdElapsed = 0f;
        while (holdElapsed < chargeHoldAfterWindup)
        {
            holdElapsed += Time.deltaTime;
            UpdateWarningRect(warn, transform.position, chargeDir, wallDist, 1f);
            yield return null;
        }
        DestroyWarningRect(warn);
        float dynSpeed = chargeSpeed + wallDist * chargeSpeedDistanceFactor;
        yield return StartCoroutine(ExecuteChargeUntilWall(chargeDir, dynSpeed, 100f,
            stunDuration: phase2WallStunDuration, shake: phase2WallShake));
        StopWingFlap();
    }

    IEnumerator EmpoweredChargeViaStun()
    {
        // Stun player
        playerIsStunned        = true;
        playerDashedDuringStun = false;
        DisablePlayerInput();

        if (healthBar != null) healthBar.Shake(playerStunDuration, 14f);

        // Orange THICK warning line (width 0.8) pointing toward player
        Vector2 chargeDir = playerTransform != null
            ? ((Vector2)playerTransform.position - (Vector2)transform.position).normalized
            : facingDir;
        if (chargeDir == Vector2.zero) chargeDir = facingDir;

        GameObject warn = CreateWarningRect(chargeDir, empoweredChargeDistance, new Color(1f, 0.5f, 0f));

        // Wait out the stun; direction tracks player, fill stays at full length
        float stunElapsed = 0f;
        while (stunElapsed < playerStunDuration)
        {
            stunElapsed += Time.deltaTime;
            if (playerTransform != null)
                chargeDir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            UpdateWarningRect(warn, transform.position, chargeDir, empoweredChargeDistance, 1f);
            yield return null;
        }

        DestroyWarningRect(warn);


        // Stun ends
        bool playerDodged      = playerDashedDuringStun;
        playerIsStunned        = false;
        playerDashedDuringStun = false;
        EnablePlayerInput();

        facingDir = chargeDir;

        StartWingFlap(wingChargeFlapSpeed);
        if (playerDodged)
        {
            // Player dodged → boss overshoots into wall
            yield return StartCoroutine(ExecuteChargeUntilWall(chargeDir,
                empoweredChargeSpeed, empoweredChargeWallMax));
            StopWingFlap();
        }
        else
        {
            // Boss charges through player
            yield return StartCoroutine(ExecuteCharge(chargeDir, empoweredChargeSpeed,
                empoweredChargeDistance, empoweredChargeDamage));
            StopWingFlap();
            yield return StartCoroutine(AfterlagSequence(phase2AfterlagDuration, chargeDir));
            yield return StartCoroutine(SmoothTurn(chargeTurnDuration));
        }
    }

    // ── Charge Execution ─────────────────────────────────────────────────────

    IEnumerator ExecuteCharge(Vector2 dir, float speed, float distance, int damage)
    {
        float moved     = 0f;
        bool  hitPlayer = false;
        isCharging      = true;

        while (moved < distance)
        {
            float step = speed * Time.fixedDeltaTime;
            moved += step;

            if (rb != null) rb.linearVelocity = dir * speed;

            // Player hit check — physical contact: distance < boss radius + player radius
            if (!hitPlayer && playerTransform != null)
            {
                float bossRadius   = sr != null ? sr.bounds.size.x * 0.5f : 0.6f;
                float contactDist  = bossRadius + chargeHitRadius; // chargeHitRadius = player radius
                if (Vector2.Distance(transform.position, playerTransform.position) < contactDist)
                {
                    hitPlayer = true;
                    DealDamageToPlayer(damage);
                    StartCoroutine(ChargeKnockbackSequence(dir));
                }
            }

            // Wall detection — stop charge early
            if (Physics2D.CircleCast(transform.position, wallCheckRadius, dir,
                step + 0.1f, wallLayerMask).collider != null)
            {
                moved = distance;
            }

            yield return new WaitForFixedUpdate();
        }

        if (rb != null) rb.linearVelocity = Vector2.zero;
        isCharging = false;
        transform.position = ClampToRoom(transform.position);
    }

    /// <summary>Charge until wall hit (used when player successfully dodges the empowered charge).</summary>
    IEnumerator ExecuteChargeUntilWall(Vector2 dir, float speed, float maxDistance,
        float stunDuration = -1f, float shake = -1f)
    {
        float moved   = 0f;
        bool  wallHit = false;
        isCharging    = true;

        bool hitPlayer = false;
        while (moved < maxDistance && !wallHit)
        {
            float   step    = speed * Time.deltaTime;
            Vector2 nextPos = (Vector2)transform.position + dir * step;
            Vector2 clamped = ClampToRoom(nextPos);

            if (Vector2.Distance(nextPos, clamped) > 0.05f)
            {
                transform.position = clamped;
                if (rb != null) rb.linearVelocity = Vector2.zero;
                wallHit = true;
                break;
            }

            if (rb != null) rb.linearVelocity = dir * speed;

            if (!hitPlayer && playerTransform != null)
            {
                float bossRadius  = sr != null ? sr.bounds.size.x * 0.5f : 0.6f;
                float contactDist = bossRadius + chargeHitRadius;
                if (Vector2.Distance(transform.position, playerTransform.position) < contactDist)
                {
                    hitPlayer = true;
                    DealDamageToPlayer(chargeDamage);
                    StartCoroutine(ChargeKnockbackSequence(dir));
                }
            }

            moved += step;
            yield return null;
        }

        if (rb != null) rb.linearVelocity = Vector2.zero;
        isCharging = false;

        if (wallHit)
        {
            // Shake on impact, then bounce
            float shakeAmt = shake >= 0f ? shake : screenShakeWallHit;
            CameraShake.Instance?.Shake(0.18f, shakeAmt);

            Vector2 bounceFrom = transform.position;
            Vector2 bounceTo   = ClampToRoom((Vector2)transform.position - dir * 1.7f);
            float   bounceT    = 0f;
            while (bounceT < 0.18f)
            {
                bounceT += Time.deltaTime;
                transform.position = Vector2.Lerp(bounceFrom, bounceTo,
                    Mathf.SmoothStep(0f, 1f, bounceT / 0.18f));
                yield return null;
            }
            transform.position = bounceTo;

            yield return StartCoroutine(WallStunSequence(stunDuration >= 0f ? stunDuration : phase2WallStunDuration));
        }
        else
        {
            transform.position = ClampToRoom(transform.position);
        }
    }

    IEnumerator WallStunSequence(float duration = -1f)
    {
        float stunDur = duration >= 0f ? duration : phase2WallStunDuration;
        backWeakPointActive  = true;
        isWallStunned        = true;
        wallStunSlashHits    = 0;
        wallStunDamageTotal  = 0;

        // Wings static at max alpha during stun
        StopWingFlap();
        if (wingCoroutine != null) { StopCoroutine(wingCoroutine); wingCoroutine = null; }
        SetWingAlpha(wingMaxAlpha);

        float wallWeakIntensity = weakPointIntensity * 1.5f;
        yield return StartCoroutine(FadeLight(backLight,
            backLight != null ? backLight.intensity : 0f, wallWeakIntensity, 0.15f));

        float stunElapsed  = 0f;
        float flickerPhase = 0f;
        while (stunElapsed < stunDur && wallStunSlashHits < 3 && wallStunDamageTotal < 8)
        {
            stunElapsed  += Time.deltaTime;
            flickerPhase += Time.deltaTime * 1f * Mathf.PI * 2f;
            float flickerT = (Mathf.Sin(flickerPhase) + 1f) * 0.5f;
            if (backLight != null) backLight.intensity = Mathf.Lerp(wallWeakIntensity * 0.35f, wallWeakIntensity, flickerT);
            yield return null;
        }

        isWallStunned       = false;
        backWeakPointActive = false;
        SetWingAlpha(0f);
        float endIntensity  = backLight != null ? backLight.intensity : 0f;
        yield return StartCoroutine(FadeLight(backLight, endIntensity, 0f, 0.2f));
        if (backLight != null) backLight.intensity = 0f;
    }

    IEnumerator SmoothTurn(float duration)
    {
        if (playerTransform == null) yield break;
        float   elapsed   = 0f;
        Vector2 startDir  = facingDir;
        // Capture target once — gives true constant angular velocity
        Vector2 targetDir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        if (targetDir == Vector2.zero) targetDir = -facingDir;
        while (elapsed < duration)
        {
            elapsed  += Time.deltaTime;
            facingDir = Vector2.Lerp(startDir, targetDir, elapsed / duration).normalized;
            yield return null;
        }
        facingDir = targetDir;
    }

    IEnumerator AfterlagSequence(float duration, Vector2 chargeDir)
    {
        backWeakPointActive = true;
        isWallStunned       = true;
        wallStunSlashHits   = 0;
        yield return StartCoroutine(FadeLight(backLight, 0f, weakPointIntensity, 0.25f));

        float elapsed      = 0f;
        float flickerPhase = 0f;
        while (elapsed < duration && wallStunSlashHits < 4)
        {
            elapsed      += Time.deltaTime;
            flickerPhase += Time.deltaTime * 1f * Mathf.PI * 2f;
            float flickerT = (Mathf.Sin(flickerPhase) + 1f) * 0.5f;
            if (backLight  != null) backLight.intensity  = Mathf.Lerp(weakPointIntensity * 0.35f, weakPointIntensity, flickerT);
            yield return null;
        }

        isWallStunned       = false;
        backWeakPointActive = false;
        float endIntensity  = backLight != null ? backLight.intensity : 0f;
        yield return StartCoroutine(FadeLight(backLight, endIntensity, 0f, 0.25f));
        if (backLight != null) backLight.intensity = 0f;
    }

    // ── Trigger Callbacks ────────────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other) => HandlePartHit(other);

    /// <summary>Called by ScarabHitForwarder on child colliders (head, wings) — knockback only, no damage.</summary>
    public void OnArmorPartHit(Collider2D other, bool isBullet)
    {
        if (state != ScarabState.InBattle) return;
        if (isBullet)
        {
            int bid = other.gameObject.GetInstanceID();
            if (bid == lastProcessedBulletID) return;
            lastProcessedBulletID = bid;
            Destroy(other.gameObject);
            SpawnFloatingText("-0", transform.position, new Color(0.6f, 0.6f, 0.6f));
            return;
        }
        // Slash: wing hit always wins — overrides body damage if both hit same frame
        if (!InitSlashFrame(other.gameObject.GetInstanceID())) return;
        pendingSlash = SlashResult.Armor;
    }

    void HandlePartHit(Collider2D other)
    {
        if (state != ScarabState.InBattle) return;

        // Player K-bullet — Bullet.cs returns early for triggers, so we handle it here
        if (other.CompareTag("Bullet"))
        {
            HandleBulletHit(other);
            return;
        }

        // Slash — identified by SlashBulletDeflector on the slash light object
        if (other.CompareTag("LightSource") && other.GetComponent<SlashBulletDeflector>() != null)
        {
            HandleSlashHit(other);
            return;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (state != ScarabState.InBattle) return;
        if (phase2TransitionActive || isAirborne) return;
        if (!other.CompareTag("Player")) return;
        if (playerInvincibleTimer > 0f || knockbackActive) return;
        if (playerTransform == null || playerRb == null) return;

        Vector2 knockDir = Vector2.Distance(transform.position, playerTransform.position) > 0.01f
            ? ((Vector2)playerTransform.position - (Vector2)transform.position).normalized
            : Vector2.up;

        DealDamageToPlayer(contactDamage);
        StartCoroutine(ChargeKnockbackSequence(knockDir));
    }

    /// <summary>Called by ScarabHitForwarder on Head/Wing child colliders. Head deals damage; wings knock back only.</summary>
    public void OnArmorPlayerContact(Vector2 partPos, bool isHead)
    {
        if (state != ScarabState.InBattle) return;
        if (phase2TransitionActive || isAirborne) return;
        if (playerInvincibleTimer > 0f || knockbackActive) return;
        if (playerTransform == null || playerRb == null) return;

        Vector2 knockDir = ((Vector2)playerTransform.position - partPos).magnitude > 0.01f
            ? ((Vector2)playerTransform.position - partPos).normalized
            : Vector2.up;

        if (isHead) DealDamageToPlayer(contactDamage);
        StartCoroutine(ChargeKnockbackSequence(knockDir));
    }

    // ── Damage Handlers ──────────────────────────────────────────────────────

    void HandleBulletHit(Collider2D bulletCol)
    {
        int bid = bulletCol.gameObject.GetInstanceID();
        if (bid == lastProcessedBulletID) return;
        lastProcessedBulletID = bid;
        Destroy(bulletCol.gameObject);

        if (IsPlayerInFront())
        {
            SpawnFloatingText("-0", transform.position, new Color(0.6f, 0.6f, 0.6f));
            StartCoroutine(HitFlashWhite());
            return;
        }

        if (!backWeakPointActive)
        {
            SpawnFloatingText("-0", transform.position, new Color(0.6f, 0.6f, 0.6f));
            return;
        }

        ApplyDamage(2);
    }

    void HandleSlashHit(Collider2D slashCol)
    {
        if (!InitSlashFrame(slashCol.gameObject.GetInstanceID())) return;
        if (pendingSlash == SlashResult.Damage) return;

        if (!IsPlayerInFront() && backWeakPointActive && pendingSlash == SlashResult.None)
        {
            pendingSlash    = SlashResult.Damage;
            pendingSlashDmg = 2;
        }
        else if (pendingSlash == SlashResult.None)
        {
            pendingSlash = SlashResult.Armor;
        }
    }

    // Returns false if this arc was already fully resolved — block re-entry from boss rotation.
    bool InitSlashFrame(int arcID)
    {
        if (arcID == lastResolvedSlashArcID) return false;
        if (pendingSlashFrame != Time.frameCount)
        {
            pendingSlashFrame = Time.frameCount;
            pendingSlash      = SlashResult.None;
            pendingSlashDmg   = 0;
            pendingSlashArcID = arcID;
        }
        return true;
    }

    void LateUpdate()
    {
        if (state != ScarabState.InBattle) return;
        if (pendingSlashFrame == Time.frameCount && pendingSlash != SlashResult.None)
            ResolveSlash();
    }

    void ResolveSlash()
    {
        if (pendingSlash == SlashResult.Damage)
        {
            ApplyDamage(pendingSlashDmg, fromSlash: true);
        }
        else if (armorKnockbackRoutine == null && !isCharging)
        {
            // Suppress if a knockback is already in flight or boss is actively charging —
            // prevents charge + armor knockback running simultaneously.
            SpawnFloatingText("-0", transform.position, new Color(0.6f, 0.6f, 0.6f));
            StartCoroutine(HitFlashWhite());
            armorKnockbackRoutine = StartCoroutine(ArmorKnockbackSequence());
        }
        lastResolvedSlashArcID = pendingSlashArcID;
        pendingSlash = SlashResult.None;
    }

    void ApplyDamage(int dmg, bool fromSlash = false)
    {
        if (phase2TransitionActive) return;

        // Lock health at phase 2 threshold and launch transition cinematic
        if (!phase2Entered && currentHealth > phase2HPThreshold
            && currentHealth - dmg <= phase2HPThreshold)
        {
            int capped = currentHealth - phase2HPThreshold;
            currentHealth = phase2HPThreshold;
            if (healthBar != null) healthBar.SetHealth(currentHealth);
            if (capped > 0)
                SpawnFloatingText("-" + capped, transform.position, new Color(1f, 0.85f, 0.2f));
            StopAllCoroutines();
            StartCoroutine(Phase2TransitionSequence());
            return;
        }

        currentHealth  = Mathf.Max(currentHealth - dmg, 0);
        if (healthBar != null) healthBar.SetHealth(currentHealth);
        SpawnFloatingText("-" + dmg, transform.position, new Color(1f, 0.08f, 0.08f));
        StartCoroutine(HitFlashWhite());
        if (isWallStunned && fromSlash) wallStunSlashHits++;
        if (isWallStunned) wallStunDamageTotal += dmg;

        if (currentHealth <= 0)
        {
            StopAllCoroutines();
            StartCoroutine(DeathSequence());
        }
    }

    void DealDamageToPlayer(int dmg)
    {
        if (playerInvincibleTimer > 0f) return;
        playerHealth?.TakeDamage(dmg);
        playerInvincibleTimer = playerInvincibleDuration;
        if (blinkRoutine != null) { StopCoroutine(blinkRoutine); if (playerSR != null) playerSR.enabled = true; }
        blinkRoutine = StartCoroutine(PlayerBlinkRoutine(playerInvincibleDuration));
    }

    bool IsPlayerInFront()
    {
        if (playerTransform == null) return false;
        Vector2 dirToPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        return Vector2.Dot(facingDir, dirToPlayer) > 0f;
    }

    IEnumerator ChargeKnockbackSequence(Vector2 chargeDir)
    {
        if (playerRb == null || playerTransform == null) yield break;

        knockbackActive = true;
        PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = false;
        playerRb.linearVelocity = Vector2.zero;

        CameraShake.Instance?.Shake(chargeKnockbackShake, chargeKnockbackShake);

        Vector2 from = playerTransform.position;
        Vector2 to   = from + chargeDir * chargeKnockbackDistance;

        float elapsed = 0f;
        while (elapsed < chargeKnockbackTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / chargeKnockbackTime);
            playerRb.MovePosition(Vector2.Lerp(from, to, t));
            yield return null;
        }
        playerRb.MovePosition(to);

        if (pm != null) pm.enabled = true;
        knockbackActive = false;
    }

    IEnumerator ArmorKnockbackSequence()
    {
        if (playerRb == null || playerTransform == null) yield break;

        PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = false;
        playerRb.linearVelocity = Vector2.zero;

        CameraShake.Instance?.Shake(armorShakeStrength, armorShakeStrength);
        if (headLight != null) StartCoroutine(HeadArmorFlash());

        Vector2 knockDir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        Vector2 from     = playerTransform.position;
        Vector2 to       = from + knockDir * armorKnockbackDistance;

        float elapsed = 0f;
        while (elapsed < armorKnockbackTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / armorKnockbackTime);
            playerRb.MovePosition(Vector2.Lerp(from, to, t));
            yield return null;
        }
        playerRb.MovePosition(to);

        if (pm != null) pm.enabled = true;
        armorKnockbackRoutine = null;
    }

    // ── Death ────────────────────────────────────────────────────────────────

    IEnumerator DeathSequence()
    {
        if (state == ScarabState.Dead) yield break;
        state = ScarabState.Dead;

        if (playerIsStunned)
        {
            playerIsStunned = false;
            EnablePlayerInput();
        }
        if (playerSR != null) playerSR.enabled = true;

        if (healthBar != null) healthBar.Hide();

        StopWingFlap();
        bossIntroCam?.FocusOnBoss(transform.position);
        DisablePlayerInput();

        // ── Step 1: flapWings亮起，backLight快速闪烁 2s ─────────────────────
        SetWingAlpha(wingMaxAlpha);

        float flashDur     = 2f;
        float flashElapsed = 0f;
        float flashPhase   = 0f;
        float flashFreq    = 6f;  // 每秒6次闪烁
        float peakIntensity = weakPointIntensity * 2f;
        if (backLight != null) backLight.intensity = peakIntensity;
        while (flashElapsed < flashDur)
        {
            flashElapsed += Time.deltaTime;
            flashPhase   += Time.deltaTime * flashFreq * Mathf.PI * 2f;
            float flickerT = (Mathf.Sin(flashPhase) + 1f) * 0.5f;
            if (backLight != null) backLight.intensity = Mathf.Lerp(peakIntensity * 0.2f, peakIntensity, flickerT);
            yield return null;
        }
        if (backLight != null) backLight.intensity = 0f;

        // ── Step 2: flapWing掉落 ─────────────────────────────────────────────
        if (flapWingLeft != null)
        {
            Vector2 vel = new Vector2(Random.Range(-1.5f, -0.5f), 0f);
            StartCoroutine(WingFallFade(flapWingLeft.gameObject, vel, 0f, 1.5f));
            flapWingLeft = null;
        }
        if (flapWingRight != null)
        {
            Vector2 vel = new Vector2(Random.Range(0.5f, 1.5f), 0f);
            StartCoroutine(WingFallFade(flapWingRight.gameObject, vel, 0f, 1.5f));
            flapWingRight = null;
        }

        yield return new WaitForSeconds(1.5f);

        // ── Step 3: 所有剩余部件（sr、head、静态wing）变暗枯黄，留在原地 ───
        var allSRs      = GetComponentsInChildren<SpriteRenderer>(true);
        var savedColors = new Color[allSRs.Length];
        for (int i = 0; i < allSRs.Length; i++) savedColors[i] = allSRs[i].color;

        Color witherColor  = new Color(0.35f, 0.28f, 0.05f);
        float witherDur    = 1.5f;
        float witherElapsed = 0f;
        while (witherElapsed < witherDur)
        {
            witherElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, witherElapsed / witherDur);
            for (int i = 0; i < allSRs.Length; i++)
            {
                if (allSRs[i] == null) continue;
                Color orig = savedColors[i];
                allSRs[i].color = new Color(
                    Mathf.Lerp(orig.r, witherColor.r, t),
                    Mathf.Lerp(orig.g, witherColor.g, t),
                    Mathf.Lerp(orig.b, witherColor.b, t),
                    orig.a);
            }
            yield return null;
        }
        yield return new WaitForSeconds(1.5f);

        // 关掉所有灯
        foreach (var light in GetComponentsInChildren<Light2D>(true))
            if (light != null) light.intensity = 0f;

        yield return new WaitForSeconds(0.3f);

        if (bossIntroCam != null && playerTransform != null)
            yield return StartCoroutine(bossIntroCam.PanBackToPlayer(playerTransform));

        EnablePlayerInput();

        if (deathPortalDelay > 0f)
            yield return new WaitForSeconds(deathPortalDelay);

        GameManager.Instance?.BossDefeated();
    }

    IEnumerator DeathThrashShake(float duration, float amplitude)
    {
        Vector2 origin  = transform.position;
        float   elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float   decay  = 1f - (elapsed / duration);
            Vector2 offset = Random.insideUnitCircle * amplitude * decay;
            transform.position = (Vector3)(origin + offset);
            yield return null;
        }
        transform.position = origin;
    }

    IEnumerator WingFallFade(GameObject wingGO, Vector2 initialVelocity, float spinDegPerSec, float duration)
    {
        if (wingGO == null) yield break;
        wingGO.transform.SetParent(null);
        var   wingSR  = wingGO.GetComponent<SpriteRenderer>();
        float startA  = wingSR != null ? wingSR.color.a : 1f;
        float elapsed = 0f;
        Vector2 vel   = initialVelocity;

        while (elapsed < duration && wingGO != null)
        {
            elapsed  += Time.deltaTime;
            vel.y    -= 6f * Time.deltaTime;  // gravity
            wingGO.transform.position += (Vector3)(vel * Time.deltaTime);
            wingGO.transform.Rotate(0f, 0f, spinDegPerSec * Time.deltaTime);
            float t = elapsed / duration;
            if (wingSR != null) { var c = wingSR.color; c.a = Mathf.Lerp(startA, 0f, t); wingSR.color = c; }
            yield return null;
        }

        if (wingGO != null) Destroy(wingGO);
    }

    // ── Phase 2 Transition Cinematic ─────────────────────────────────────────

    IEnumerator Phase2TransitionSequence()
    {
        phase2TransitionActive = true;
        phase2Entered          = true;
        isAttacking            = true;
        isCharging             = false;

        StopWingFlap();
        if (blinkRoutine != null) { StopCoroutine(blinkRoutine); blinkRoutine = null; }
        if (playerSR != null) playerSR.enabled = true;
        playerInvincibleTimer = 0f;
        DisablePlayerInput();

        Vector2 bossPos = transform.position;

        // ── Shockwave + screen shake + player knockback ─────────────────────
        CameraShake.Instance?.Shake(0.8f, 0.5f);
        StartCoroutine(P2Shockwave(bossPos, p2ShockwaveRadius, p2ShockwaveDuration));

        if (playerRb != null && playerTransform != null)
        {
            Vector2 knockDir = ((Vector2)playerTransform.position - bossPos).normalized;
            if (knockDir == Vector2.zero) knockDir = Vector2.up;
            Vector2 from = playerTransform.position;
            Vector2 to   = ClampToRoom(from + knockDir * p2KnockbackDistance);
            float   t    = 0f;
            while (t < p2KnockbackTime)
            {
                t += Time.deltaTime;
                playerRb.MovePosition(Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / p2KnockbackTime)));
                yield return null;
            }
            playerRb.MovePosition(to);
        }

        yield return new WaitForSeconds(1.3f);  // ~1.5s total gap from shockwave

        // ── Boss hovers briefly then ascends ─────────────────────────────────
        Vector3 savedScale  = transform.localScale;
        float   warnRadius  = GetWarnWidth() * 0.5f; // capture before scale changes
        StartWingFlap(wingChargeFlapSpeed);

        // Grow larger while fading out — stop the WingFlapLoop so it doesn't fight the manual flap below
        StopWingFlap();
        if (wingCoroutine != null) { StopCoroutine(wingCoroutine); wingCoroutine = null; }

        var srs = GetComponentsInChildren<SpriteRenderer>();
        var savedColors = new Color[srs.Length];
        for (int i = 0; i < srs.Length; i++) savedColors[i] = srs[i].color;
        Color savedFlapL = flapWingLeft  != null ? flapWingLeft.color  : Color.clear;
        Color savedFlapR = flapWingRight != null ? flapWingRight.color : Color.clear;

        var   lights        = GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>();
        var   savedLightInt = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) savedLightInt[i] = lights[i].intensity;

        Vector2 camCenter = Camera.main != null
            ? (Vector2)Camera.main.transform.position : (Vector2)transform.position;

        float growDur   = 0.7f;
        float growT     = 0f;
        float flapPhase = 0f;
        Vector2 growFrom = transform.position;
        while (growT < growDur)
        {
            growT     += Time.deltaTime;
            flapPhase += Time.deltaTime * wingChargeFlapSpeed * Mathf.PI * 2f;
            float ease    = Mathf.SmoothStep(0f, 1f, growT / growDur);
            float blackT  = Mathf.Clamp01(ease / 0.7f);          // 0→1 over first 70%
            float fadeT   = Mathf.Clamp01((ease - 0.7f) / 0.3f); // 0→1 over last 30%
            transform.localScale = Vector3.Lerp(savedScale, savedScale * 4f, ease);
            transform.position   = Vector2.Lerp(growFrom, camCenter, ease * 0.35f);

            // Sprites: RGB → black over first 70%, alpha → 0 over last 30%
            for (int i = 0; i < srs.Length; i++)
            {
                Color orig = savedColors[i];
                Color c    = orig;
                c.r = Mathf.Lerp(orig.r, 0f, blackT);
                c.g = Mathf.Lerp(orig.g, 0f, blackT);
                c.b = Mathf.Lerp(orig.b, 0f, blackT);
                c.a = Mathf.Lerp(orig.a, 0f, fadeT);
                srs[i].color = c;
            }

            // Flap wings: same black→fade treatment, with sine oscillation on alpha
            float flapT  = (Mathf.Sin(flapPhase) + 1f) * 0.5f;
            float flapBaseA = Mathf.Lerp(wingMinAlpha, wingMaxAlpha, flapT);
            void ApplyFlapColor(SpriteRenderer sr, Color orig)
            {
                if (sr == null) return;
                Color c = orig;
                c.r = Mathf.Lerp(orig.r, 0f, blackT);
                c.g = Mathf.Lerp(orig.g, 0f, blackT);
                c.b = Mathf.Lerp(orig.b, 0f, blackT);
                c.a = flapBaseA * (1f - fadeT);
                sr.color = c;
            }
            ApplyFlapColor(flapWingLeft,  savedFlapL);
            ApplyFlapColor(flapWingRight, savedFlapR);
            yield return null;
        }
        for (int i = 0; i < srs.Length; i++) { var c = srs[i].color; c.a = 0f; srs[i].color = c; }
        if (flapWingLeft  != null) { var c = flapWingLeft.color;  c.a = 0f; flapWingLeft.color  = c; }
        if (flapWingRight != null) { var c = flapWingRight.color; c.a = 0f; flapWingRight.color = c; }
        for (int i = 0; i < lights.Length; i++) lights[i].intensity = 0f;

        // ── Release player, wait before warning ─────────────────────────────
        EnablePlayerInput();
        yield return new WaitForSeconds(p2LandingDelay);

        // Capture landing position: wherever player currently is
        Vector2 landPos = playerTransform != null
            ? ClampToRoom((Vector2)playerTransform.position)
            : ClampToRoom(bossPos);

        // Boss invisible off-screen — start circle, wait 2/3 tracking player, then appear enlarged
        Vector2 offScreenPos      = landPos + Vector2.up * (roomHalfSize.y + 10f);
        float   transitionDiveDur = Vector2.Distance(offScreenPos, landPos) / p2DiveSpeed + diveTimeBuffer;
        transform.position   = offScreenPos;
        transform.localScale = savedScale;

        StartCoroutine(P2LandingWarning(landPos, warnRadius, transitionDiveDur));

        // Track player direction during first 2/3 of circle
        float waitAppear = transitionDiveDur * 2f / 3f;
        float appearElapsed = 0f;
        while (appearElapsed < waitAppear)
        {
            appearElapsed += Time.deltaTime;
            if (playerTransform != null)
            {
                Vector2 toPlayer = ((Vector2)playerTransform.position - landPos).normalized;
                if (toPlayer != Vector2.zero) facingDir = toPlayer;
            }
            yield return null;
        }

        // Boss appears enlarged at landPos — black, semi-transparent, 2x scale
        Vector3 landScale   = savedScale * 2f;
        const float startAlpha = 0.35f;
        transform.position   = landPos;
        transform.localScale = landScale;
        for (int i = 0; i < srs.Length; i++)
        {
            Color c = savedColors[i]; c.r = 0f; c.g = 0f; c.b = 0f; c.a = startAlpha * c.a;
            srs[i].color = c;
        }
        if (flapWingLeft  != null) { Color c = savedFlapL; c.r = 0f; c.g = 0f; c.b = 0f; c.a = startAlpha * wingMinAlpha; flapWingLeft.color  = c; }
        if (flapWingRight != null) { Color c = savedFlapR; c.r = 0f; c.g = 0f; c.b = 0f; c.a = startAlpha * wingMinAlpha; flapWingRight.color = c; }
        for (int i = 0; i < lights.Length; i++) lights[i].intensity = 0f;
        StartWingFlap(wingChaseFlapSpeed);

        // Scale to normal + de-blacken + fade alpha in over remaining 1/3
        float remainTime  = transitionDiveDur / 3f;
        float scaleT      = 0f;
        float flapPhase2  = 0f;
        while (scaleT < remainTime)
        {
            scaleT     += Time.deltaTime;
            flapPhase2 += Time.deltaTime * wingChaseFlapSpeed * Mathf.PI * 2f;
            float t      = Mathf.Clamp01(scaleT / remainTime);
            float alphaT = t;                              // 0→1 alpha
            float colorT = Mathf.Clamp01((t - 0.3f) / 0.7f); // de-blacken starts at 30%
            transform.localScale = Vector3.Lerp(landScale, savedScale, Mathf.SmoothStep(0f, 1f, t));
            for (int i = 0; i < srs.Length; i++)
            {
                Color orig = savedColors[i];
                Color c    = orig;
                c.r = Mathf.Lerp(0f, orig.r, colorT);
                c.g = Mathf.Lerp(0f, orig.g, colorT);
                c.b = Mathf.Lerp(0f, orig.b, colorT);
                c.a = Mathf.Lerp(startAlpha * orig.a, orig.a, alphaT);
                srs[i].color = c;
            }
            float flapT2 = (Mathf.Sin(flapPhase2) + 1f) * 0.5f;
            float flapA2 = Mathf.Lerp(wingMinAlpha, wingMaxAlpha, flapT2);
            if (flapWingLeft  != null) { Color c = savedFlapL; c.r = Mathf.Lerp(0f, savedFlapL.r, colorT); c.g = Mathf.Lerp(0f, savedFlapL.g, colorT); c.b = Mathf.Lerp(0f, savedFlapL.b, colorT); c.a = Mathf.Lerp(startAlpha * wingMinAlpha, flapA2, alphaT); flapWingLeft.color  = c; }
            if (flapWingRight != null) { Color c = savedFlapR; c.r = Mathf.Lerp(0f, savedFlapR.r, colorT); c.g = Mathf.Lerp(0f, savedFlapR.g, colorT); c.b = Mathf.Lerp(0f, savedFlapR.b, colorT); c.a = Mathf.Lerp(startAlpha * wingMinAlpha, flapA2, alphaT); flapWingRight.color = c; }
            for (int i = 0; i < lights.Length; i++) lights[i].intensity = Mathf.Lerp(0f, savedLightInt[i], alphaT);
            yield return null;
        }
        transform.localScale = savedScale;
        for (int i = 0; i < srs.Length; i++) srs[i].color = savedColors[i];
        if (flapWingLeft  != null) flapWingLeft.color  = savedFlapL;
        if (flapWingRight != null) flapWingRight.color = savedFlapR;
        for (int i = 0; i < lights.Length; i++) lights[i].intensity = savedLightInt[i];

        // Circle damage check — player inside circle when it fills
        if (playerTransform != null &&
            Vector2.Distance(landPos, playerTransform.position) <= warnRadius)
        {
            DealDamageToPlayer(2);
            Vector2 knockDir = ((Vector2)playerTransform.position - landPos).normalized;
            if (knockDir == Vector2.zero) knockDir = Vector2.up;
            yield return StartCoroutine(ChargeKnockbackSequence(knockDir));
        }

        // ── Landing impact ───────────────────────────────────────────────────
        CameraShake.Instance?.Shake(0.6f, 0.5f);

        EnablePlayerInput();
        phase2TransitionActive = false;
        isAttacking            = true;
        yield return StartCoroutine(DiveFollowUpCharge());
        yield return StartCoroutine(P2TakeoffSequence());
        isAttacking = false;
        StartCoroutine(BattleLoop(immediate: true));
    }

    IEnumerator P2Shockwave(Vector2 center, float maxRadius, float duration)
    {
        const int seg = 40;
        var go  = new GameObject("P2Shockwave");
        var lr  = go.AddComponent<LineRenderer>();
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.loop          = true;
        lr.positionCount = seg;
        lr.startWidth    = 0.45f;
        lr.endWidth      = 0.45f;
        lr.sortingOrder  = 5;

        const float expandFrac = 0.7f; // spend 70% expanding, 30% holding+fading
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float radius, alpha, width;
            if (t < expandFrac)
            {
                float et = t / expandFrac;
                radius = maxRadius * et;
                alpha  = 1f;
                width  = Mathf.Lerp(0.45f, 0.2f, et);
            }
            else
            {
                float ft = (t - expandFrac) / (1f - expandFrac);
                radius = maxRadius;
                alpha  = 1f - ft;
                width  = 0.2f;
            }
            lr.startWidth = width; lr.endWidth = width;
            Color col = new Color(1f, 0.3f, 0.1f, alpha);
            lr.startColor = col; lr.endColor = col;
            SetCirclePoints(lr, center, radius, seg);
            yield return null;
        }
        Destroy(go);
    }


    IEnumerator P2LandingWarning(Vector2 center, float maxRadius, float duration)
    {
        const int seg = 48;

        // Fixed outer ring
        var ringGo = new GameObject("P2LandingRing");
        var lr = ringGo.AddComponent<LineRenderer>();
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.loop          = true;
        lr.positionCount = seg;
        lr.startWidth    = 0.2f;
        lr.endWidth      = 0.2f;
        lr.sortingOrder  = 11;
        lr.startColor    = new Color(1f, 0.08f, 0.08f, 1f);
        lr.endColor      = new Color(1f, 0.08f, 0.08f, 1f);
        SetCirclePoints(lr, center, maxRadius, seg);

        // Growing filled disk
        var fillGo = new GameObject("P2LandingFill");
        fillGo.transform.position = new Vector3(center.x, center.y, 0f);
        var mf   = fillGo.AddComponent<MeshFilter>();
        var mr   = fillGo.AddComponent<MeshRenderer>();
        mr.material     = new Material(Shader.Find("Sprites/Default"));
        mr.sortingOrder = 10;
        var mesh = new Mesh();
        mf.mesh  = mesh;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t      = elapsed / duration;
            float radius = maxRadius * t;

            var verts = new Vector3[seg + 1];
            var tris  = new int[seg * 3];
            verts[0] = Vector3.zero;
            for (int i = 0; i < seg; i++)
            {
                float angle = i * Mathf.PI * 2f / seg;
                verts[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            }
            for (int i = 0; i < seg; i++)
            {
                tris[i * 3]     = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = (i + 1) % seg + 1;
            }
            mesh.vertices  = verts;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            mr.material.color = new Color(1f, 0.08f, 0.08f, 0.45f);

            yield return null;
        }

        Destroy(ringGo);
        Destroy(fillGo);
    }

void SetCirclePoints(LineRenderer lr, Vector2 center, float radius, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * 2f * Mathf.PI;
            lr.SetPosition(i, new Vector3(
                center.x + Mathf.Cos(angle) * radius,
                center.y + Mathf.Sin(angle) * radius, 0f));
        }
    }

    // ── Input Control ────────────────────────────────────────────────────────

    void DisablePlayerInput()
    {
        if (playerTransform == null) return;
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;
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
        foreach (var mb in playerTransform.GetComponents<MonoBehaviour>())
        {
            if (mb is PlayerMovement || mb is PlayerShooting || mb is PlayerSlash ||
                mb is PlayerDash     || mb is PlayerLightWave || mb is FlashlightAim)
                mb.enabled = true;
        }
    }

    // ── Visual Helpers ───────────────────────────────────────────────────────

    IEnumerator HeadArmorFlash()
    {
        if (headLight == null) yield break;
        headLight.intensity = headFlashIntensity;
        yield return StartCoroutine(FadeLight(headLight, headFlashIntensity, 0f, 0.25f));
    }

    IEnumerator HitFlashWhite()
    {
        yield break;
    }

    IEnumerator PlayerBlinkRoutine(float duration)
    {
        if (playerSR == null) yield break;
        float elapsed = 0f;
        bool  visible = true;
        while (elapsed < duration)
        {
            visible          = !visible;
            playerSR.enabled = visible;
            yield return new WaitForSeconds(playerBlinkInterval);
            elapsed += playerBlinkInterval;
        }
        playerSR.enabled = true;
    }

    IEnumerator FadeLight(Light2D light, float from, float to, float duration)
    {
        if (light == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed       += Time.deltaTime;
            light.intensity = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        light.intensity = to;
    }

    IEnumerator FadeLightRadius(Light2D light, float from, float to, float duration)
    {
        if (light == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            light.pointLightOuterRadius = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        light.pointLightOuterRadius = to;
    }

    // ── Warning Rect ─────────────────────────────────────────────────────────
    // Layout: two thin white side lines show the full danger zone width from
    // the start. A solid red/orange fill expands forward from the boss over
    // the windup duration. No front cap — the fill endpoint tells the player
    // how far the charge will reach once windup completes.

    float GetWarnWidth()
    {
        float w = chargeWarningWidth;
        var allR = GetComponentsInChildren<SpriteRenderer>();
        if (allR.Length > 0)
        {
            Bounds b = allR[0].bounds;
            foreach (var r in allR) b.Encapsulate(r.bounds);
            w = Mathf.Max(b.size.x, b.size.y) * chargeWarningWidthScale;
        }
        return w;
    }

    // Returns a container GameObject; pass fillT=0 to start with empty fill.
    GameObject CreateWarningRect(Vector2 dir, float totalLength, Color fillColor)
    {
        var mat = new Material(Shader.Find("Sprites/Default"));

        var container = new GameObject("ScarabWarnRect");

        // Side lines (white, thin, full length from the start)
        foreach (string side in new[] { "SideL", "SideR" })
        {
            var sGO = new GameObject(side);
            sGO.transform.SetParent(container.transform);
            var lr         = sGO.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth  = 0.06f;
            lr.endWidth    = 0.06f;
            lr.alignment   = LineAlignment.TransformZ;
            lr.material    = mat;
            lr.startColor  = new Color(1f, 1f, 1f, 0.35f);
            lr.endColor    = new Color(1f, 1f, 1f, 0.35f);
            lr.sortingOrder = 5;
        }

        // Fill (wide solid, expands forward)
        var fGO = new GameObject("Fill");
        fGO.transform.SetParent(container.transform);
        var fill         = fGO.AddComponent<LineRenderer>();
        fill.positionCount = 2;
        fill.startWidth  = 0f; // set dynamically by UpdateWarningRect
        fill.endWidth    = 0f;
        fill.alignment   = LineAlignment.TransformZ;
        fill.material    = mat;
        fill.startColor  = new Color(fillColor.r, fillColor.g, fillColor.b, 0.22f);
        fill.endColor    = new Color(fillColor.r, fillColor.g, fillColor.b, 0.22f);
        fill.sortingOrder = 4;

        UpdateWarningRect(container, transform.position, dir, totalLength, 0f);
        return container;
    }

    void UpdateWarningRect(GameObject rect, Vector2 origin, Vector2 dir, float totalLength, float fillT)
    {
        if (rect == null) return;
        float   halfW = GetWarnWidth() * 0.5f;
        Vector2 perp  = new Vector2(-dir.y, dir.x);

        var sideL = rect.transform.Find("SideL")?.GetComponent<LineRenderer>();
        if (sideL != null)
        {
            sideL.SetPosition(0, (Vector3)(origin - perp * halfW));
            sideL.SetPosition(1, (Vector3)(origin + dir * totalLength - perp * halfW));
        }

        var sideR = rect.transform.Find("SideR")?.GetComponent<LineRenderer>();
        if (sideR != null)
        {
            sideR.SetPosition(0, (Vector3)(origin + perp * halfW));
            sideR.SetPosition(1, (Vector3)(origin + dir * totalLength + perp * halfW));
        }

        var fill = rect.transform.Find("Fill")?.GetComponent<LineRenderer>();
        if (fill != null)
        {
            float fillLen  = totalLength * Mathf.Clamp01(fillT);
            float fillW    = halfW * 2f;
            fill.startWidth = fillW;
            fill.endWidth   = fillW;
            fill.SetPosition(0, (Vector3)origin);
            fill.SetPosition(1, (Vector3)(origin + dir * fillLen));
        }
    }

    void DestroyWarningRect(GameObject rect)
    {
        if (rect != null) Destroy(rect);
    }

    // ── Utility ──────────────────────────────────────────────────────────────

    float DistanceToWall(Vector2 pos, Vector2 dir)
    {
        float margin = 0.85f;
        float minX = roomCenter.x - roomHalfSize.x * margin;
        float maxX = roomCenter.x + roomHalfSize.x * margin;
        float minY = roomCenter.y - roomHalfSize.y * margin;
        float maxY = roomCenter.y + roomHalfSize.y * margin;

        float tMin = float.MaxValue;
        if (Mathf.Abs(dir.x) > 0.001f)
        {
            float t = dir.x > 0f ? (maxX - pos.x) / dir.x : (minX - pos.x) / dir.x;
            if (t > 0f) tMin = Mathf.Min(tMin, t);
        }
        if (Mathf.Abs(dir.y) > 0.001f)
        {
            float t = dir.y > 0f ? (maxY - pos.y) / dir.y : (minY - pos.y) / dir.y;
            if (t > 0f) tMin = Mathf.Min(tMin, t);
        }
        return tMin == float.MaxValue ? 20f : tMin;
    }

    Vector2 ClampToRoom(Vector2 pos)
    {
        float margin = 0.85f;
        pos.x = Mathf.Clamp(pos.x,
            roomCenter.x - roomHalfSize.x * margin,
            roomCenter.x + roomHalfSize.x * margin);
        pos.y = Mathf.Clamp(pos.y,
            roomCenter.y - roomHalfSize.y * margin,
            roomCenter.y + roomHalfSize.y * margin);
        return pos;
    }

    static void SpawnFloatingText(string text, Vector3 worldPos, Color color)
    {
        var go = new GameObject("ScarabDmgText");
        float xOff = Random.Range(-0.25f, 0.25f);
        float yOff = Random.Range(0.4f,   0.7f);
        go.transform.position = worldPos + new Vector3(xOff, yOff, 0f);

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text         = text;
        tmp.fontSize     = 14f;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.color        = new Color(color.r * 0.75f, color.g * 0.75f, color.b * 0.75f, color.a);
        tmp.outlineWidth = 0.4f;
        tmp.outlineColor = new Color32(0, 0, 0, 255);
        tmp.sortingOrder = 100;
        go.AddComponent<FloatingTextFader>();
    }
}
