using UnityEngine;

/// <summary>
/// Draws a mini health bar above an enemy using SpriteRenderers.
/// Created entirely in code — no prefab or sprites needed.
/// Add this to a GameObject that is a child of the enemy,
/// OR call EnemyHealthBar.AttachTo(enemyGameObject) to set it up automatically.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    // ── Layout ────────────────────────────────────────────────────────────────
    private const float BarWidth    = 1.1f;   // world units wide
    private const float BarHeight   = 0.16f;  // world units tall
    private const float YOffset     = 0.85f;  // above the enemy pivot
    private const int   SortOrder   = 20;     // renders above enemy sprite

    // ── Colour scheme ─────────────────────────────────────────────────────────
    private static readonly Color BgColor   = new Color(0.1f, 0.1f, 0.1f, 0.85f);
    private static readonly Color FullColor = new Color(0.25f, 0.95f, 0.35f, 1f);  // green
    private static readonly Color MidColor  = new Color(1f,   0.80f, 0.1f,  1f);  // yellow
    private static readonly Color LowColor  = new Color(1f,   0.2f,  0.1f,  1f);  // red

    // ── Internal refs ─────────────────────────────────────────────────────────
    private SpriteRenderer bgSR;
    private SpriteRenderer fillSR;
    private Transform      fillTransform;
    private float          currentAlpha;
    private float          targetAlpha;

    private const float FadeInSpeed  = 8f;
    private const float FadeOutSpeed = 4f;

    // ─────────────────────────────────────────────────────────────────────────────
    /// <summary>Creates and attaches a health bar to the given enemy GameObject.</summary>
    public static EnemyHealthBar AttachTo(GameObject enemy, int maxHP)
    {
        var barRoot = new GameObject("HealthBar");
        barRoot.transform.SetParent(enemy.transform, false);
        barRoot.transform.localPosition = new Vector3(0f, YOffset, 0f);

        var hb = barRoot.AddComponent<EnemyHealthBar>();
        hb.Build(maxHP);
        return hb;
    }

    // ── Setup ─────────────────────────────────────────────────────────────────
    void Build(int maxHP)
    {
        Sprite whiteSprite = MakeWhiteSprite();

        // ── Background ────────────────────────────────────────────────────────
        var bg = new GameObject("BG");
        bg.transform.SetParent(transform, false);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale    = new Vector3(BarWidth, BarHeight, 1f);

        bgSR = bg.AddComponent<SpriteRenderer>();
        bgSR.sprite       = whiteSprite;
        bgSR.color        = BgColor;
        bgSR.sortingOrder = SortOrder;

        // ── Fill ──────────────────────────────────────────────────────────────
        // Pivot at left edge so we can scale X without moving the bar right
        var fill = new GameObject("Fill");
        fill.transform.SetParent(transform, false);
        // Shift left by half bar width so left edge aligns with bg left edge
        fill.transform.localPosition = new Vector3(-BarWidth * 0.5f, 0f, -0.01f);
        fill.transform.localScale    = new Vector3(BarWidth, BarHeight * 0.75f, 1f);

        fillSR = fill.AddComponent<SpriteRenderer>();
        fillSR.sprite       = whiteSprite;
        fillSR.color        = FullColor;
        fillSR.sortingOrder = SortOrder + 1;

        // Store fill transform with pivot shifted to left
        // We'll set pivot via a child trick
        var pivot = new GameObject("FillPivot");
        pivot.transform.SetParent(transform, false);
        pivot.transform.localPosition = new Vector3(-BarWidth * 0.5f, 0f, -0.01f);

        fill.transform.SetParent(pivot.transform, true);
        fillTransform = pivot.transform;

        // Start fully filled but hidden by default.
        gameObject.SetActive(true);
        currentAlpha = 0f;
        targetAlpha = 0f;
        ApplyAlpha(0f);
        SetFill(maxHP, maxHP);
    }

    void Update()
    {
        float speed = targetAlpha > currentAlpha ? FadeInSpeed : FadeOutSpeed;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, speed * Time.deltaTime);
        ApplyAlpha(currentAlpha);
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void SetFill(int current, int max)
    {
        if (fillTransform == null || fillSR == null || bgSR == null) return;

        float pct = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

        // Scale X from 0→1 (pivot is at left edge, so it grows right)
        Vector3 s = fillTransform.localScale;
        fillTransform.localScale = new Vector3(pct, s.y, s.z);

        // Colour: green → yellow → red
        Color fillColor = pct > 0.6f ? FullColor
                        : pct > 0.3f ? Color.Lerp(MidColor, FullColor, (pct - 0.3f) / 0.3f)
                        :               Color.Lerp(LowColor,  MidColor, pct / 0.3f);
        fillColor.a = currentAlpha;
        fillSR.color = fillColor;
    }

    public void SetVisible(bool visible, bool instant = false)
    {
        targetAlpha = visible ? 1f : 0f;
        if (instant)
        {
            currentAlpha = targetAlpha;
            ApplyAlpha(currentAlpha);
        }
    }

    void ApplyAlpha(float alpha)
    {
        if (bgSR == null || fillSR == null) return;

        var bgColor = bgSR.color;
        bgColor.a = BgColor.a * alpha;
        bgSR.color = bgColor;

        var fillColor = fillSR.color;
        fillColor.a = alpha;
        fillSR.color = fillColor;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static Sprite MakeWhiteSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0f, 0.5f), 1f);
        // pivot x=0 (left edge), y=0.5 (vertical centre)
    }
}
