using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Boss health bar displayed at the top-center of the screen.
/// Built entirely in code — no prefab required.
/// Call Initialize(max) once, then SetHealth(hp) on each damage event.
/// Call Show() to fade the bar in; it starts hidden.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private float barWidthFraction = 0.6f;   // fraction of 1920 ref width
    [SerializeField] private float barHeight        = 28f;
    [SerializeField] private float topOffset        = 40f;    // pixels down from top
    [SerializeField] private Color fillColor   = new Color(0.85f, 0.08f, 0.08f, 1f);
    [SerializeField] private Color bgColor     = new Color(0.08f, 0.08f, 0.08f, 0.85f);
    [SerializeField] private float fadeInDuration  = 0.2f;

    private CanvasGroup  canvasGroup;
    private Image        fillImage;
    private float        maxHealth;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        BuildUI();
        // Start fully hidden
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void Initialize(float max)
    {
        maxHealth = Mathf.Max(max, 0.001f);
        SetFill(1f);
    }

    public void SetHealth(float hp)
    {
        float fraction = Mathf.Clamp01(hp / maxHealth);
        SetFill(fraction);
    }

    /// <summary>Fade the bar into view over fadeInDuration seconds.</summary>
    public void Show()
    {
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    void SetFill(float t)
    {
        if (fillImage != null)
            fillImage.fillAmount = t;
    }

    IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        canvasGroup.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    void BuildUI()
    {
        // Canvas
        var canvasObj = new GameObject("BossHealthBarCanvas");
        canvasObj.transform.SetParent(transform, false);

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha        = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Background bar — top-center, 60% screen width
        float refWidth = 1920f * barWidthFraction;

        var bgObj = new GameObject("HPBarBackground");
        bgObj.transform.SetParent(canvasObj.transform, false);
        var bgRT = bgObj.AddComponent<RectTransform>();
        bgRT.anchorMin        = new Vector2(0.5f, 1f);
        bgRT.anchorMax        = new Vector2(0.5f, 1f);
        bgRT.pivot            = new Vector2(0.5f, 1f);
        bgRT.anchoredPosition = new Vector2(0f, -topOffset);
        bgRT.sizeDelta        = new Vector2(refWidth, barHeight);
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color        = bgColor;
        bgImg.raycastTarget = false;

        // Fill bar (child of background so it respects its bounds)
        var fillObj = new GameObject("HPBarFill");
        fillObj.transform.SetParent(bgObj.transform, false);
        var fillRT = fillObj.AddComponent<RectTransform>();
        fillRT.anchorMin   = Vector2.zero;
        fillRT.anchorMax   = Vector2.one;
        fillRT.offsetMin   = new Vector2(3f, 3f);
        fillRT.offsetMax   = new Vector2(-3f, -3f);

        fillImage = fillObj.AddComponent<Image>();
        fillImage.color        = fillColor;
        fillImage.type         = Image.Type.Filled;
        fillImage.fillMethod   = Image.FillMethod.Horizontal;
        fillImage.fillOrigin   = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount   = 1f;
        fillImage.raycastTarget = false;
    }
}
