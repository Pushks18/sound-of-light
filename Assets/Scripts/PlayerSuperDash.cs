using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// K + L special: player charges at the boss, deals 50 damage on contact,
/// and launches the boss far across the arena.
///
/// Visual language: electric blue-white (distinct from boomerang red).
///   - Pre-launch: wide blue corridor + charge-up glow on player
///   - Launch trail: bright white streaks behind player
///   - Impact: blinding white-blue burst + expanding shockwave + boss flies away with orange trail
///   - Boss slams far wall with camera shake
/// Cooldown: 10 s.
/// </summary>
public class PlayerSuperDash : MonoBehaviour
{
    [Header("Super Dash Stats")]
    [SerializeField] float dashSpeed       = 55f;
    [SerializeField] float bossDamage      = 50f;
    [SerializeField] float hitRadius       = 2.2f;
    [SerializeField] float knockbackDist   = 14f;   // how far boss flies
    [SerializeField] float cooldown        = 10f;
    [SerializeField] float windupDur       = 0.35f;

    [Header("Path Visual")]
    [SerializeField] float pathWidth       = 2.0f;
    [SerializeField] float pathFadeDur     = 0.45f;

    [Header("Impact FX")]
    [SerializeField] float impactShake     = 0.55f;
    [SerializeField] float impactLightR    = 9f;

    // ── State ─────────────────────────────────────────────────────────────────

    bool  isActive;
    float cooldownTimer;

    Rigidbody2D    rb;
    SpriteRenderer sr;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        bool kDown = Input.GetKeyDown(KeyCode.K);
        bool lDown = Input.GetKeyDown(KeyCode.L);
        bool kHeld = Input.GetKey(KeyCode.K);
        bool lHeld = Input.GetKey(KeyCode.L);

        // Fire when either key goes down while the other is already held
        bool triggered = (kDown && lHeld) || (kDown && lHeld);

        if (!isActive && cooldownTimer <= 0f && triggered)
            TryLaunch();
    }

    // ── Launch ────────────────────────────────────────────────────────────────

    void TryLaunch()
    {
        var boss = FindFirstObjectByType<ShadowBossAI>();
        if (boss == null || !boss.IsInBattle) return;

        StartCoroutine(SuperDashSequence(boss));
    }

    IEnumerator SuperDashSequence(ShadowBossAI boss)
    {
        isActive      = true;
        cooldownTimer = cooldown;

        Vector2 originPos = transform.position;
        Vector2 bossPos   = boss.transform.position;
        Vector2 dashDir   = (bossPos - originPos).normalized;

        SetInputEnabled(false);
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // ── Build path indicator ──────────────────────────────────────────────
        var pathRoot = BuildGroundPath(originPos, bossPos, dashDir);

        // ── Charge-up windup: player glows electric blue ──────────────────────
        yield return StartCoroutine(WindupEffect(windupDur));

        // ── Player dash toward boss ───────────────────────────────────────────
        bool hitLanded = false;
        float elapsed  = 0f;
        float maxTime  = 4f;   // safety timeout

        while (elapsed < maxTime)
        {
            elapsed += Time.deltaTime;

            if (boss == null) break;

            Vector2 toBoss = (Vector2)boss.transform.position - (Vector2)transform.position;
            float   dist   = toBoss.magnitude;

            // Hit registered when close enough
            if (!hitLanded && dist <= hitRadius)
            {
                hitLanded = true;
                MovePlayer(boss.transform.position);

                // Destroy path, run impact
                if (pathRoot != null) { Destroy(pathRoot); pathRoot = null; }
                yield return StartCoroutine(ImpactSequence(boss, dashDir));
                break;
            }

            // Move toward boss
            Vector2 step = toBoss.normalized * dashSpeed * Time.deltaTime;
            MovePlayer((Vector2)transform.position + step);

            // Spawn trail light
            SpawnTrailLight(transform.position);

            yield return null;
        }

        // ── End ───────────────────────────────────────────────────────────────
        if (pathRoot != null) StartCoroutine(FadeDestroyPath(pathRoot));
        SetInputEnabled(true);
        if (rb != null) rb.linearVelocity = Vector2.zero;
        isActive = false;
    }

    // ── Impact sequence ───────────────────────────────────────────────────────

    IEnumerator ImpactSequence(ShadowBossAI boss, Vector2 dashDir)
    {
        // Giant white-blue burst at impact point
        StartCoroutine(ImpactBurst(transform.position));
        StartCoroutine(ImpactRingWaves(transform.position));

        CameraShake.Instance?.Shake(impactShake, impactShake);

        // Brief freeze frame (hitstop)
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(0.09f);
        Time.timeScale = 1f;

        // Camera zoom punch
        StartCoroutine(ImpactZoomPunch());

        // Launch boss
        if (boss != null)
            yield return StartCoroutine(boss.TakeSuperDashHit(bossDamage, dashDir, knockbackDist));

        // Player slides back slightly from recoil
        yield return StartCoroutine(PlayerRecoil(dashDir));
    }

    IEnumerator PlayerRecoil(Vector2 dashDir)
    {
        Vector2 recoilFrom = transform.position;
        Vector2 recoilTo   = recoilFrom - dashDir * 2.2f;
        float   dur        = 0.18f, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            MovePlayer(Vector2.Lerp(recoilFrom, recoilTo, Mathf.SmoothStep(0f, 1f, t / dur)));
            yield return null;
        }
    }

    // ── FX Coroutines ─────────────────────────────────────────────────────────

    IEnumerator ImpactBurst(Vector2 pos)
    {
        var go = new GameObject("SuperDashImpact");
        go.transform.position = pos;

        var l                   = go.AddComponent<Light2D>();
        l.lightType             = Light2D.LightType.Point;
        l.color                 = new Color(0.7f, 0.88f, 1f);
        l.intensity             = 14f;
        l.pointLightOuterRadius = impactLightR;
        l.pointLightInnerRadius = impactLightR * 0.1f;
        l.shadowsEnabled        = false;

        float dur = 0.55f, t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            l.intensity = Mathf.Lerp(14f, 0f, Mathf.Pow(t / dur, 0.5f));
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator ImpactRingWaves(Vector2 center)
    {
        // Three expanding rings with slight delay between each
        for (int w = 0; w < 3; w++)
        {
            StartCoroutine(SingleRing(center, 6f + w * 2.5f, 0.45f,
                new Color(0.5f, 0.85f, 1f, 1f)));
            yield return new WaitForSeconds(0.07f);
        }
    }

    IEnumerator SingleRing(Vector2 center, float maxR, float dur, Color col)
    {
        const int seg = 36;
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) yield break;

        var go = new GameObject("SuperDashRing");
        var lr = go.AddComponent<LineRenderer>();
        lr.material      = new Material(shader);
        lr.loop          = true;
        lr.positionCount = seg;
        lr.sortingOrder  = 9;

        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t     = elapsed / dur;
            float r     = maxR * t;
            float alpha = 1f - t;
            float w     = Mathf.Lerp(0.50f, 0.06f, t);
            lr.startWidth = w; lr.endWidth = w;
            Color c = new Color(col.r, col.g, col.b, alpha);
            lr.startColor = c; lr.endColor = c;
            for (int i = 0; i < seg; i++)
            {
                float a = (float)i / seg * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(center.x + Mathf.Cos(a) * r,
                                              center.y + Mathf.Sin(a) * r, 0f));
            }
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator ImpactZoomPunch()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        float orig    = cam.orthographicSize;
        float zoomIn  = orig * 0.78f;
        float zoomOut = orig * 1.12f;

        // Punch in fast
        float t = 0f;
        while (t < 0.08f) { t += Time.deltaTime; cam.orthographicSize = Mathf.Lerp(orig, zoomIn, t / 0.08f); yield return null; }
        // Push out
        t = 0f;
        while (t < 0.22f) { t += Time.deltaTime; cam.orthographicSize = Mathf.Lerp(zoomIn, zoomOut, t / 0.22f); yield return null; }
        // Return
        t = 0f;
        while (t < 0.30f) { t += Time.deltaTime; cam.orthographicSize = Mathf.Lerp(zoomOut, orig, t / 0.30f); yield return null; }
        cam.orthographicSize = orig;
    }

    // ── Ground Path ───────────────────────────────────────────────────────────

    GameObject BuildGroundPath(Vector2 origin, Vector2 bossPos, Vector2 dashDir)
    {
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) return null;

        var root = new GameObject("SuperDashPath");

        // Main corridor: electric blue-white
        AddCorridorLine(root, "Main", origin, bossPos,
            new Color(0.35f, 0.80f, 1f, 0.85f),
            new Color(0.70f, 0.95f, 1f, 0.40f),
            pathWidth, shader, 4);

        // White hot inner core (narrower, brighter)
        AddCorridorLine(root, "Core", origin, bossPos,
            new Color(1f, 1f, 1f, 0.90f),
            new Color(0.8f, 0.95f, 1f, 0.30f),
            pathWidth * 0.28f, shader, 6);

        // Edge borders
        Vector2 perp = new Vector2(-dashDir.y, dashDir.x) * (pathWidth * 0.5f);
        AddEdgeLine(root, "EdgeL", origin - perp, bossPos - perp, shader);
        AddEdgeLine(root, "EdgeR", origin + perp, bossPos + perp, shader);

        // Boss target crosshair
        StartCoroutine(BossTargetCrosshair(root, bossPos));

        // Chevron arrows along path
        StartCoroutine(SpawnChevrons(root, origin, bossPos, dashDir));

        // Glow lights
        StartCoroutine(CorridorGlowLights(root, origin, bossPos));

        // Knockback arrow beyond boss
        StartCoroutine(KnockbackArrow(root, bossPos, dashDir, shader));

        return root;
    }

    void AddCorridorLine(GameObject root, string name, Vector2 from, Vector2 to,
                         Color cStart, Color cEnd, float width, Shader shader, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        var lr           = go.AddComponent<LineRenderer>();
        lr.material      = new Material(shader);
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth    = width;
        lr.endWidth      = width;
        lr.startColor    = cStart;
        lr.endColor      = cEnd;
        lr.sortingOrder  = order;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
    }

    void AddEdgeLine(GameObject root, string name, Vector2 from, Vector2 to, Shader shader)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        var lr           = go.AddComponent<LineRenderer>();
        lr.material      = new Material(shader);
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth    = 0.07f; lr.endWidth = 0.07f;
        Color c = new Color(0.8f, 0.95f, 1f, 0.75f);
        lr.startColor = c; lr.endColor = c;
        lr.sortingOrder = 7;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
    }

    // Pulsing crosshair at boss position
    IEnumerator BossTargetCrosshair(GameObject root, Vector2 center)
    {
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) yield break;

        float armLen = 1.4f;
        string[] arms = { "ArmH", "ArmV" };
        var lrs = new LineRenderer[2];

        for (int i = 0; i < 2; i++)
        {
            var go = new GameObject(arms[i]);
            go.transform.SetParent(root.transform, true);
            var lr = go.AddComponent<LineRenderer>();
            lr.material      = new Material(shader);
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.startWidth    = 0.12f; lr.endWidth = 0.12f;
            lr.sortingOrder  = 9;
            lrs[i] = lr;
        }
        // Horizontal arm
        lrs[0].SetPosition(0, center + Vector2.left  * armLen);
        lrs[0].SetPosition(1, center + Vector2.right * armLen);
        // Vertical arm
        lrs[1].SetPosition(0, center + Vector2.down * armLen);
        lrs[1].SetPosition(1, center + Vector2.up   * armLen);

        // Rotating circle around crosshair
        const int seg = 24;
        var circGo = new GameObject("TargetCircle");
        circGo.transform.SetParent(root.transform, true);
        var clr = circGo.AddComponent<LineRenderer>();
        clr.material      = new Material(shader);
        clr.loop          = true;
        clr.positionCount = seg;
        clr.sortingOrder  = 9;
        float cR = 1.0f;
        for (int i = 0; i < seg; i++)
        {
            float a = (float)i / seg * Mathf.PI * 2f;
            clr.SetPosition(i, new Vector3(center.x + Mathf.Cos(a) * cR,
                                           center.y + Mathf.Sin(a) * cR, 0f));
        }

        float elapsed = 0f;
        while (root != null)
        {
            elapsed += Time.deltaTime;
            float pulse = (Mathf.Sin(elapsed * 7f) + 1f) * 0.5f;
            Color c = new Color(0.4f, 0.85f, 1f, Mathf.Lerp(0.5f, 1f, pulse));
            float w = Mathf.Lerp(0.08f, 0.22f, pulse);
            for (int i = 0; i < 2; i++) { lrs[i].startColor = c; lrs[i].endColor = c; lrs[i].startWidth = w; lrs[i].endWidth = w; }
            clr.startColor = c; clr.endColor = c; clr.startWidth = w; clr.endWidth = w;

            // Rotate circle
            float rotR = cR * 1.05f;
            float rotOff = elapsed * 1.8f;
            for (int i = 0; i < seg; i++)
            {
                float a = (float)i / seg * Mathf.PI * 2f + rotOff;
                clr.SetPosition(i, new Vector3(center.x + Mathf.Cos(a) * rotR,
                                               center.y + Mathf.Sin(a) * rotR, 0f));
            }
            yield return null;
        }
    }

    // Dashed knock-back arrow beyond the boss showing where it'll fly
    IEnumerator KnockbackArrow(GameObject root, Vector2 bossPos, Vector2 dir, Shader shader)
    {
        float   arrowLen = 5f;
        Vector2 arrowEnd = bossPos + dir * arrowLen;
        Color   col      = new Color(1f, 0.5f, 0.1f, 0.70f);

        var go = new GameObject("KnockArrow");
        go.transform.SetParent(root.transform, false);
        var lr           = go.AddComponent<LineRenderer>();
        lr.material      = new Material(shader);
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth    = 0.25f; lr.endWidth = 0.05f;
        lr.startColor    = col;
        lr.endColor      = new Color(col.r, col.g, col.b, 0f);
        lr.sortingOrder  = 5;
        lr.SetPosition(0, bossPos);
        lr.SetPosition(1, arrowEnd);

        // Pulse
        float elapsed = 0f;
        while (root != null && go != null)
        {
            elapsed += Time.deltaTime;
            float alpha = (Mathf.Sin(elapsed * 5f) + 1f) * 0.5f * 0.80f;
            lr.startColor = new Color(col.r, col.g, col.b, alpha);
            yield return null;
        }
    }

    IEnumerator SpawnChevrons(GameObject root, Vector2 origin, Vector2 bossPos, Vector2 dir)
    {
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) yield break;

        Vector2 perp    = new Vector2(-dir.y, dir.x);
        float   totalD  = Vector2.Distance(origin, bossPos);
        float   spacing = 2.2f;
        int     count   = Mathf.Max(1, Mathf.FloorToInt(totalD / spacing));

        for (int i = 1; i <= count; i++)
        {
            float   t   = (float)i / (count + 1);
            Vector2 mid = Vector2.Lerp(origin, bossPos, t);
            float   sz  = 0.45f;

            var go = new GameObject("Chev_" + i);
            go.transform.SetParent(root.transform, true);
            var lr = go.AddComponent<LineRenderer>();
            lr.material      = new Material(shader);
            lr.useWorldSpace = true;
            lr.positionCount = 3;
            lr.startWidth    = 0.13f; lr.endWidth = 0.13f;
            Color col = new Color(0.75f, 0.95f, 1f, 0.90f);
            lr.startColor = col; lr.endColor = col;
            lr.sortingOrder = 8;

            lr.SetPosition(0, mid - perp * sz - dir * (sz * 0.6f));
            lr.SetPosition(1, mid + dir * sz);
            lr.SetPosition(2, mid + perp * sz - dir * (sz * 0.6f));

            yield return new WaitForSeconds(0.018f);
        }
    }

    IEnumerator CorridorGlowLights(GameObject root, Vector2 origin, Vector2 bossPos)
    {
        float totalD = Vector2.Distance(origin, bossPos);
        int   count  = Mathf.Max(3, Mathf.FloorToInt(totalD / 2.2f));

        for (int i = 0; i <= count; i++)
        {
            float   t   = (float)i / count;
            Vector2 pos = Vector2.Lerp(origin, bossPos, t);

            var go = new GameObject("GlowDot_" + i);
            go.transform.SetParent(root.transform, true);
            go.transform.position = pos;

            var l                   = go.AddComponent<Light2D>();
            l.lightType             = Light2D.LightType.Point;
            l.color                 = new Color(0.4f, 0.82f, 1f);
            l.intensity             = 2.0f;
            l.pointLightOuterRadius = 2.0f;
            l.pointLightInnerRadius = 0.3f;
            l.shadowsEnabled        = false;

            StartCoroutine(PulseLight(l, i * 0.06f));
            yield return null;
        }
    }

    IEnumerator PulseLight(Light2D l, float offset)
    {
        float e = offset;
        while (l != null)
        {
            e += Time.deltaTime;
            if (l != null) l.intensity = Mathf.Lerp(0.7f, 2.8f, (Mathf.Sin(e * 5f) + 1f) * 0.5f);
            yield return null;
        }
    }

    void SpawnTrailLight(Vector2 pos)
    {
        var go = new GameObject("SuperDashTrail");
        go.transform.position = pos;

        var l                   = go.AddComponent<Light2D>();
        l.lightType             = Light2D.LightType.Point;
        l.color                 = new Color(0.55f, 0.88f, 1f);
        l.intensity             = 4f;
        l.pointLightOuterRadius = 1.4f;
        l.shadowsEnabled        = false;

        StartCoroutine(FadeTrailLight(l));
    }

    IEnumerator FadeTrailLight(Light2D l)
    {
        float dur = 0.3f, t = 0f;
        while (t < dur && l != null)
        {
            t += Time.deltaTime;
            if (l != null) l.intensity = Mathf.Lerp(4f, 0f, t / dur);
            yield return null;
        }
        if (l != null) Destroy(l.gameObject);
    }

    // ── Windup ────────────────────────────────────────────────────────────────

    IEnumerator WindupEffect(float dur)
    {
        Color orig = sr != null ? sr.color : Color.white;
        float e = 0f;

        // Player builds up a blue-white electric charge
        while (e < dur)
        {
            e += Time.deltaTime;
            float t     = e / dur;
            float pulse = Mathf.Sin(t * Mathf.PI * 5f);  // rapid flickering
            if (sr != null) sr.color = Color.Lerp(orig, new Color(0.6f, 0.9f, 1f, 1f), Mathf.Abs(pulse));

            // Spawn tiny static sparks
            if (Random.value < 0.4f) SpawnTrailLight(transform.position);

            yield return null;
        }

        if (sr != null) sr.color = orig;
    }

    // ── Path helpers ──────────────────────────────────────────────────────────

    void FadePathAlpha(GameObject pathRoot, float alpha)
    {
        if (pathRoot == null) return;
        foreach (var lr in pathRoot.GetComponentsInChildren<LineRenderer>())
        {
            Color sc = lr.startColor; sc.a *= alpha; lr.startColor = sc;
            Color ec = lr.endColor;   ec.a *= alpha; lr.endColor   = ec;
        }
        foreach (var l2d in pathRoot.GetComponentsInChildren<Light2D>())
            l2d.intensity *= alpha;
    }

    IEnumerator FadeDestroyPath(GameObject pathRoot)
    {
        float e = 0f;
        while (e < pathFadeDur && pathRoot != null)
        {
            e += Time.deltaTime;
            FadePathAlpha(pathRoot, 1f - e / pathFadeDur);
            yield return null;
        }
        if (pathRoot != null) Destroy(pathRoot);
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    void MovePlayer(Vector2 pos)
    {
        if (rb != null) rb.MovePosition(pos);
        else            transform.position = pos;
    }

    void SetInputEnabled(bool on)
    {
        foreach (var mb in GetComponents<MonoBehaviour>())
        {
            if (mb == this) continue;
            if (mb is PlayerMovement || mb is PlayerShooting || mb is PlayerSlash ||
                mb is PlayerDash     || mb is PlayerLightWave || mb is FlashlightAim)
                mb.enabled = on;
        }
        if (!on && rb != null) rb.linearVelocity = Vector2.zero;
    }
}
