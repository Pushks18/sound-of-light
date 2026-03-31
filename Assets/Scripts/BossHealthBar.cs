using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Boss health bar displayed at the top-center of the screen.
/// Built entirely in code — no prefab required.
/// Call Initialize(max) once, then SetHealth(hp) on each damage event.
/// Call Show() to fade the bar in; it starts hidden.
///
/// Hit-impact effect: when health drops, the lost segment turns white and
/// drains away after a short delay, giving a visual punch on every hit.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private float barWidthFraction = 0.6f;   // fraction of 1920 ref width
    [SerializeField] private float barHeight        = 28f;
    [SerializeField] private float topOffset        = 40f;    // pixels down from top
    [SerializeField] private Color fillColor   = new Color(0.85f, 0.08f, 0.08f, 1f);
    [SerializeField] private Color bgColor     = new Color(0.08f, 0.08f, 0.08f, 0.85f);
    [SerializeField] private Color ghostColor  = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private float fadeInDuration   = 0.2f;
    [SerializeField] private float fadeOutDuration  = 0.4f;

    [Header("Hit Impact Effect")]
    [SerializeField] private float ghostDrainDelay = 0.35f;   // seconds before white bar starts draining
    [SerializeField] private float ghostDrainSpeed = 0.55f;   // fill units per second

    private CanvasGroup  canvasGroup;
    private Image        fillImage;
    private Image        ghostImage;    // white bar sitting behind main fill
    private RectTransform bgRT;         // background rect — used for shake
    private float        maxHealth;
    private float        currentFill  = 1f;
    private float        ghostFill    = 1f;
    private float        ghostTimer   = 0f;   // counts down; drains when <= 0

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        BuildUI();
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (ghostImage == null) return;
        if (ghostFill <= currentFill + 0.001f) return;   // nothing to drain

        if (ghostTimer > 0f)
        {
            ghostTimer -= Time.deltaTime;
            return;
        }

        // Drain ghost fill toward current fill
        ghostFill = Mathf.MoveTowards(ghostFill, currentFill, ghostDrainSpeed * Time.deltaTime);
        ghostImage.fillAmount = ghostFill;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void Initialize(float max)
    {
        maxHealth    = Mathf.Max(max, 0.001f);
        currentFill  = 1f;
        ghostFill    = 1f;
        ghostTimer   = 0f;
        if (fillImage  != null) fillImage.fillAmount  = 1f;
        if (ghostImage != null) ghostImage.fillAmount = 1f;
    }

    public void SetHealth(float hp)
    {
        float newFill = Mathf.Clamp01(hp / maxHealth);
        if (newFill >= currentFill)
        {
            // Heal or no change — snap both bars immediately
            currentFill = newFill;
            ghostFill   = newFill;
            if (fillImage  != null) fillImage.fillAmount  = currentFill;
            if (ghostImage != null) ghostImage.fillAmount = ghostFill;
            return;
        }

        // Damage — main bar snaps down, ghost holds then drains
        currentFill = newFill;
        if (fillImage != null) fillImage.fillAmount = currentFill;

        // Ghost stays at old value; restart drain delay
        ghostTimer = ghostDrainDelay;
        // ghostFill keeps its current value (already >= currentFill)
    }

    /// <summary>Fade the bar into view over fadeInDuration seconds.</summary>
    public void Show()
    {
        StopAllCoroutines();
        StartCoroutine(FadeTo(1f, fadeInDuration));
    }

    /// <summary>Fade the bar out of view over fadeOutDuration seconds.</summary>
    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(FadeTo(0f, fadeOutDuration));
    }

    /// <summary>Shake the health bar for a short duration — used on phase transition.</summary>
    public void Shake(float duration = 0.5f, float magnitude = 6f)
    {
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        if (bgRT == null) yield break;
        Vector2 originalPos = bgRT.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentMag = Mathf.Lerp(magnitude, 0f, elapsed / duration);
            bgRT.anchoredPosition = originalPos + Random.insideUnitCircle * currentMag;
            yield return null;
        }
        bgRT.anchoredPosition = originalPos;
    }

    IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float elapsed    = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
    }

    void BuildUI()
    {
        // ── Canvas ──────────────────────────────────────────────────────────
        var canvasObj = new GameObject("BossHealthBarCanvas");
        canvasObj.transform.SetParent(transform, false);

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha          = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // ── Background ───────────────────────────────────────────────────────
        float refWidth = 1920f * barWidthFraction;

        var bgObj = new GameObject("HPBarBackground");
        bgObj.transform.SetParent(canvasObj.transform, false);
        bgRT = bgObj.AddComponent<RectTransform>();
        bgRT.anchorMin        = new Vector2(0.5f, 1f);
        bgRT.anchorMax        = new Vector2(0.5f, 1f);
        bgRT.pivot            = new Vector2(0.5f, 1f);
        bgRT.anchoredPosition = new Vector2(0f, -topOffset);
        bgRT.sizeDelta        = new Vector2(refWidth, barHeight);
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color         = bgColor;
        bgImg.raycastTarget = false;

        Sprite whiteSprite = CreateWhiteSprite();

        // ── Ghost fill (white, drains after hit) — behind main fill ──────────
        var ghostObj = new GameObject("HPBarGhost");
        ghostObj.transform.SetParent(bgObj.transform, false);
        var ghostRT = ghostObj.AddComponent<RectTransform>();
        ghostRT.anchorMin  = Vector2.zero;
        ghostRT.anchorMax  = Vector2.one;
        ghostRT.offsetMin  = new Vector2(3f, 3f);
        ghostRT.offsetMax  = new Vector2(-3f, -3f);

        ghostImage              = ghostObj.AddComponent<Image>();
        ghostImage.sprite       = whiteSprite;
        ghostImage.color        = ghostColor;
        ghostImage.type         = Image.Type.Filled;
        ghostImage.fillMethod   = Image.FillMethod.Horizontal;
        ghostImage.fillOrigin   = (int)Image.OriginHorizontal.Left;
        ghostImage.fillAmount   = 1f;
        ghostImage.raycastTarget = false;

        // ── Main fill (red, snaps on hit) — in front of ghost ────────────────
        var fillObj = new GameObject("HPBarFill");
        fillObj.transform.SetParent(bgObj.transform, false);
        var fillRT = fillObj.AddComponent<RectTransform>();
        fillRT.anchorMin  = Vector2.zero;
        fillRT.anchorMax  = Vector2.one;
        fillRT.offsetMin  = new Vector2(3f, 3f);
        fillRT.offsetMax  = new Vector2(-3f, -3f);

        fillImage              = fillObj.AddComponent<Image>();
        fillImage.sprite       = whiteSprite;
        fillImage.color        = fillColor;
        fillImage.type         = Image.Type.Filled;
        fillImage.fillMethod   = Image.FillMethod.Horizontal;
        fillImage.fillOrigin   = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount   = 1f;
        fillImage.raycastTarget = false;
    }

    /// <summary>
    /// Creates a 1x1 white sprite at runtime so Image.Type.Filled works
    /// reliably without requiring a sprite asset in the project.
    /// </summary>
    static Sprite CreateWhiteSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f);
    }
}
