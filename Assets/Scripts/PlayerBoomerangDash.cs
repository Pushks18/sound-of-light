using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// J + K special: player rockets through the boss (same distance past it), deals 40 damage,
/// boss is completely frozen for the whole move, then player returns to origin.
///
/// Trajectory: origin → overshoot (boss + mirror distance past boss) → origin
/// Example: player 5 units from boss → player travels 5 units to boss, 5 units past, returns 10 units home.
///
/// Path: wide red corridor drawn on the ground for the full route.
/// Boss: stunned (all AI paused, velocity zeroed) for the duration.
/// </summary>
public class PlayerBoomerangDash : MonoBehaviour
{
    [Header("Boomerang Stats")]
    [SerializeField] float dashSpeed     = 40f;
    [SerializeField] float returnSpeed   = 34f;
    [SerializeField] float bossDamage    = 40f;
    [SerializeField] float hitRadius     = 2.0f;   // within this distance → register the hit
    [SerializeField] float cooldown      = 8f;
    [SerializeField] float windupDur     = 0.28f;  // pre-launch glow

    [Header("Path Visual")]
    [SerializeField] float pathWidth     = 1.8f;   // wide red corridor
    [SerializeField] float pathFadeDur   = 0.5f;

    [Header("Impact FX")]
    [SerializeField] float impactShake   = 0.40f;
    [SerializeField] float impactLightR  = 7f;

    // ── State ─────────────────────────────────────────────────────────────────

    bool  isActive;
    float cooldownTimer;
    bool  bothKeysHeld;

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

        bool jk = Input.GetKey(KeyCode.J) && Input.GetKey(KeyCode.K);

        if (!isActive && cooldownTimer <= 0f && jk && !bothKeysHeld)
            TryLaunch();

        bothKeysHeld = jk;
    }

    // ── Launch ────────────────────────────────────────────────────────────────

    void TryLaunch()
    {
        var boss = FindFirstObjectByType<ShadowBossAI>();
        if (boss == null || !boss.IsInBattle) return;

        StartCoroutine(BoomerangSequence(boss));
    }

    IEnumerator BoomerangSequence(ShadowBossAI boss)
    {
        isActive      = true;
        cooldownTimer = cooldown;

        Vector2 originPos = transform.position;
        Vector2 bossPos   = boss.transform.position;
        Vector2 dir       = (bossPos - originPos).normalized;
        float   dist      = Vector2.Distance(originPos, bossPos);

        // Overshoot: same distance past the boss as player is from boss
        Vector2 overshootPos = bossPos + dir * dist;

        // Lock all other player input
        SetInputEnabled(false);
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Freeze boss for the entire move
        float totalDur = windupDur
                       + (dist * 2f) / dashSpeed          // out to overshoot
                       + Vector2.Distance(overshootPos, originPos) / returnSpeed  // back
                       + 0.15f;                            // small buffer
        StartCoroutine(boss.SpecialStun(totalDur));

        // ── Ground path — full corridor from origin through boss to overshoot ──
        var pathRoot = BuildGroundPath(originPos, overshootPos, bossPos);

        // ── Windup ────────────────────────────────────────────────────────────
        yield return StartCoroutine(WindupEffect(windupDur));

        // ── Outbound: origin → overshoot (hits boss along the way) ───────────
        bool hitLanded    = false;
        float outDist     = Vector2.Distance(originPos, overshootPos);
        float outElapsed  = 0f;
        float outDur      = outDist / dashSpeed;

        while (outElapsed < outDur)
        {
            outElapsed += Time.deltaTime;
            float   t    = Mathf.Clamp01(outElapsed / outDur);
            Vector2 pos  = Vector2.Lerp(originPos, overshootPos, t);
            MovePlayer(pos);

            // Register hit when we pass close to boss
            if (!hitLanded && boss != null
                && Vector2.Distance(pos, boss.transform.position) <= hitRadius)
            {
                hitLanded = true;
                RegisterImpact(boss);
            }

            // Shrink the outbound segment of the path behind the player
            UpdatePathOutTail(pathRoot, pos);

            yield return null;
        }

        MovePlayer(overshootPos);

        // ── Brief pause at overshoot ──────────────────────────────────────────
        yield return new WaitForSeconds(0.06f);

        // ── Return: overshoot → origin ────────────────────────────────────────
        float returnDist    = Vector2.Distance(overshootPos, originPos);
        float returnDur     = returnDist / returnSpeed;
        float returnElapsed = 0f;

        while (returnElapsed < returnDur)
        {
            returnElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, returnElapsed / returnDur);
            MovePlayer(Vector2.Lerp(overshootPos, originPos, t));
            FadePathAlpha(pathRoot, 1f - t * 0.6f);   // partial fade during return
            yield return null;
        }

        MovePlayer(originPos);

        // ── Cleanup ───────────────────────────────────────────────────────────
        if (pathRoot != null) StartCoroutine(FadeDestroyPath(pathRoot));
        SetInputEnabled(true);
        if (rb != null) rb.linearVelocity = Vector2.zero;
        isActive = false;
    }

    // ── Impact ────────────────────────────────────────────────────────────────

    void RegisterImpact(ShadowBossAI boss)
    {
        boss.TakeBoomerangDamage(bossDamage);
        CameraShake.Instance?.Shake(impactShake, impactShake);
        StartCoroutine(ImpactLightBurst(boss.transform.position));
    }

    IEnumerator ImpactLightBurst(Vector2 pos)
    {
        var go = new GameObject("BoomerangImpact");
        go.transform.position = pos;

        var l                   = go.AddComponent<Light2D>();
        l.lightType             = Light2D.LightType.Point;
        l.color                 = new Color(1f, 0.25f, 0.04f);
        l.intensity             = 10f;
        l.pointLightOuterRadius = impactLightR;
        l.pointLightInnerRadius = impactLightR * 0.15f;
        l.shadowsEnabled        = false;

        // Also spawn a ring shockwave
        StartCoroutine(ImpactRing(pos));

        float dur = 0.5f, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            l.intensity = Mathf.Lerp(10f, 0f, t / dur);
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator ImpactRing(Vector2 center)
    {
        const int  seg  = 32;
        float      maxR = 5f;
        float      dur  = 0.35f;

        var shader = Shader.Find("Sprites/Default");
        if (shader == null) yield break;

        var go = new GameObject("BoomerangRing");
        var lr = go.AddComponent<LineRenderer>();
        lr.material      = new Material(shader);
        lr.loop          = true;
        lr.positionCount = seg;
        lr.sortingOrder  = 8;

        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t     = elapsed / dur;
            float r     = maxR * t;
            float alpha = 1f - t;
            float w     = Mathf.Lerp(0.55f, 0.08f, t);
            lr.startWidth = w; lr.endWidth = w;
            Color c = new Color(1f, 0.2f, 0.05f, alpha);
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

    // ── Windup FX ─────────────────────────────────────────────────────────────

    IEnumerator WindupEffect(float dur)
    {
        Color origColor = sr != null ? sr.color : Color.white;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.PingPong(elapsed * 10f, 1f);
            if (sr != null) sr.color = Color.Lerp(origColor, new Color(1f, 0.15f, 0.05f, 1f), pulse);
            yield return null;
        }
        if (sr != null) sr.color = origColor;
    }

    // ── Ground Path — wide corridor ───────────────────────────────────────────

    GameObject BuildGroundPath(Vector2 origin, Vector2 overshoot, Vector2 bossPos)
    {
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) return null;

        var root = new GameObject("BoomerangPath");

        // ── Main corridor: full-width solid red lane ─────────────────────────
        // Outbound half (origin → overshoot)
        AddCorridorSegment(root, "MainOut", origin, overshoot,
            new Color(1f, 0.05f, 0.02f, 0.85f),
            new Color(1f, 0.3f, 0.05f, 0.55f),
            pathWidth, shader, sortOrder: 4);

        // ── Edge lines: bright white border on both sides ────────────────────
        Vector2 perp = Perp((overshoot - origin).normalized) * (pathWidth * 0.5f);
        AddSideLine(root, "EdgeL_out", origin - perp, overshoot - perp, shader);
        AddSideLine(root, "EdgeR_out", origin + perp, overshoot + perp, shader);

        // ── Boss marker: pulsing red ring at boss position ───────────────────
        StartCoroutine(BossTargetRing(root, bossPos));

        // ── Arrow chevrons along the path ────────────────────────────────────
        StartCoroutine(SpawnChevrons(root, origin, overshoot, bossPos));

        // ── Glow lights along corridor ────────────────────────────────────────
        StartCoroutine(CorridorGlowLights(root, origin, overshoot));

        return root;
    }

    // Adds a thick LineRenderer corridor segment
    void AddCorridorSegment(GameObject root, string name, Vector2 from, Vector2 to,
                            Color startCol, Color endCol, float width, Shader shader, int sortOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);

        var lr           = go.AddComponent<LineRenderer>();
        lr.material      = new Material(shader);
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth    = width;
        lr.endWidth      = width;
        lr.startColor    = startCol;
        lr.endColor      = endCol;
        lr.sortingOrder  = sortOrder;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
    }

    void AddSideLine(GameObject root, string name, Vector2 from, Vector2 to, Shader shader)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);

        var lr           = go.AddComponent<LineRenderer>();
        lr.material      = new Material(shader);
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth    = 0.08f;
        lr.endWidth      = 0.08f;
        Color c = new Color(1f, 0.9f, 0.85f, 0.70f);
        lr.startColor = c; lr.endColor = c;
        lr.sortingOrder = 6;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
    }

    // Pulsing target ring at boss position
    IEnumerator BossTargetRing(GameObject root, Vector2 center)
    {
        const int seg  = 28;
        float     r    = 1.8f;
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) yield break;

        var go = new GameObject("BossTarget");
        go.transform.SetParent(root.transform, true);
        var lr = go.AddComponent<LineRenderer>();
        lr.material      = new Material(shader);
        lr.loop          = true;
        lr.positionCount = seg;
        lr.sortingOrder  = 8;

        for (int i = 0; i < seg; i++)
        {
            float a = (float)i / seg * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(center.x + Mathf.Cos(a) * r,
                                          center.y + Mathf.Sin(a) * r, 0f));
        }

        float elapsed = 0f;
        while (go != null && root != null)
        {
            elapsed += Time.deltaTime;
            float pulse = (Mathf.Sin(elapsed * 6f) + 1f) * 0.5f;
            float w     = Mathf.Lerp(0.12f, 0.35f, pulse);
            lr.startWidth = w; lr.endWidth = w;
            Color c = new Color(1f, 0.05f, 0.02f, Mathf.Lerp(0.6f, 1f, pulse));
            lr.startColor = c; lr.endColor = c;
            yield return null;
        }
    }

    // Arrow chevrons pointing along the path direction
    IEnumerator SpawnChevrons(GameObject root, Vector2 origin, Vector2 overshoot, Vector2 bossPos)
    {
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) yield break;

        Vector2 dir  = (overshoot - origin).normalized;
        Vector2 perp = Perp(dir);
        float   spacing = 2.5f;
        float   totalDist = Vector2.Distance(origin, overshoot);
        int     count = Mathf.Max(1, Mathf.FloorToInt(totalDist / spacing));

        for (int i = 1; i <= count; i++)
        {
            float t = (float)i / (count + 1);
            Vector2 mid = Vector2.Lerp(origin, overshoot, t);
            float size = 0.5f;

            var go = new GameObject("Chevron_" + i);
            go.transform.SetParent(root.transform, true);
            var lr = go.AddComponent<LineRenderer>();
            lr.material      = new Material(shader);
            lr.useWorldSpace = true;
            lr.positionCount = 3;
            lr.startWidth    = 0.14f;
            lr.endWidth      = 0.14f;
            Color col = new Color(1f, 0.85f, 0.7f, 0.90f);
            lr.startColor = col; lr.endColor = col;
            lr.sortingOrder = 7;

            // V-shape pointing in direction of travel
            lr.SetPosition(0, mid - perp * size - dir * (size * 0.6f));
            lr.SetPosition(1, mid + dir * size);
            lr.SetPosition(2, mid + perp * size - dir * (size * 0.6f));

            yield return new WaitForSeconds(0.02f);
        }
    }

    // Soft glow lights floating above the corridor floor
    IEnumerator CorridorGlowLights(GameObject root, Vector2 origin, Vector2 overshoot)
    {
        float totalDist = Vector2.Distance(origin, overshoot);
        int   count     = Mathf.Max(3, Mathf.FloorToInt(totalDist / 2.5f));

        for (int i = 0; i <= count; i++)
        {
            float   t   = (float)i / count;
            Vector2 pos = Vector2.Lerp(origin, overshoot, t);

            var go = new GameObject("CorrLight_" + i);
            go.transform.SetParent(root.transform, true);
            go.transform.position = pos;

            var l                   = go.AddComponent<Light2D>();
            l.lightType             = Light2D.LightType.Point;
            l.color                 = new Color(1f, 0.18f, 0.04f);
            l.intensity             = 1.8f;
            l.pointLightOuterRadius = 2.2f;
            l.pointLightInnerRadius = 0.4f;
            l.shadowsEnabled        = false;

            StartCoroutine(PulseCorridorLight(l, i * 0.07f));
            yield return null;
        }
    }

    IEnumerator PulseCorridorLight(Light2D l, float phaseOffset)
    {
        float elapsed = phaseOffset;
        while (l != null)
        {
            elapsed += Time.deltaTime;
            float pulse = (Mathf.Sin(elapsed * 5f) + 1f) * 0.5f;
            if (l != null) l.intensity = Mathf.Lerp(0.8f, 2.5f, pulse);
            yield return null;
        }
    }

    // ── Path tail update (shrink outbound line as player moves) ───────────────

    void UpdatePathOutTail(GameObject pathRoot, Vector2 playerPos)
    {
        if (pathRoot == null) return;
        var seg = pathRoot.transform.Find("MainOut");
        if (seg == null) return;
        var lr = seg.GetComponent<LineRenderer>();
        if (lr != null) lr.SetPosition(0, playerPos);
    }

    void FadePathAlpha(GameObject pathRoot, float alpha)
    {
        if (pathRoot == null) return;
        foreach (var lr in pathRoot.GetComponentsInChildren<LineRenderer>())
        {
            Color sc = lr.startColor; sc.a = sc.a * alpha; lr.startColor = sc;
            Color ec = lr.endColor;   ec.a = ec.a * alpha; lr.endColor   = ec;
        }
        foreach (var l2d in pathRoot.GetComponentsInChildren<Light2D>())
            l2d.intensity *= alpha;
    }

    IEnumerator FadeDestroyPath(GameObject pathRoot)
    {
        float elapsed = 0f;
        while (elapsed < pathFadeDur && pathRoot != null)
        {
            elapsed += Time.deltaTime;
            FadePathAlpha(pathRoot, 1f - elapsed / pathFadeDur);
            yield return null;
        }
        if (pathRoot != null) Destroy(pathRoot);
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    static Vector2 Perp(Vector2 v) => new Vector2(-v.y, v.x);

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
