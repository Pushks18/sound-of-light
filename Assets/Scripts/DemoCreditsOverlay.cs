using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fullscreen "Thank You" + scrolling team-names credits overlay.
/// Spawned by DemoSequenceManager when the demo sequence completes.
///
/// Builds its UI procedurally so no scene wiring is required. Persists across
/// scene loads (via DontDestroyOnLoad set by the manager) so it can render on
/// top of whatever scene was active when the last boss died.
/// </summary>
public class DemoCreditsOverlay : MonoBehaviour
{
    /// <summary>Invoked when the credits sequence finishes (after the final fade-out).</summary>
    public Action OnFinished;

    static readonly string[] TeamMembers = new string[]
    {
        "Rachel Channell",
        "Yunzhe Li",
        "Jackson Xie",
        "Pushkaraj Baradkar",
        "Praveen Saravanan",
    };

    // Timing
    const float BackgroundFadeDuration = 1.4f;
    const float TitleFadeDuration      = 0.9f;
    const float TitleHold              = 1.6f;
    const float NameFadeIn             = 0.45f;
    const float NameHold               = 1.6f;
    const float NameFadeOut            = 0.4f;
    const float TitleFadeOutDuration   = 0.8f;
    const float FinalHoldBeforeMenu    = 1.0f;

    // Colors
    static readonly Color BgBlack      = new Color(0f, 0f, 0f, 0f);
    static readonly Color AccentYellow = new Color(1f, 0.823f, 0.290f, 0f); // #ffd24a
    static readonly Color NameColor    = new Color(0.902f, 0.902f, 0.941f, 0f); // #e6e6f0

    Image bg;
    Text titleText;
    Text nameText;

    void Start()
    {
        BuildUI();
        StartCoroutine(PlayCredits());
    }

    void BuildUI()
    {
        // Top-level Canvas
        var canvasGO = new GameObject("CreditsCanvas");
        canvasGO.transform.SetParent(transform);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Black background that fades in
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bg = bgGO.AddComponent<Image>();
        bg.color = BgBlack;
        bg.raycastTarget = false;

        var fontResource = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // "Thank You." title
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(canvasGO.transform, false);
        var titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.65f);
        titleRect.anchorMax = new Vector2(0.5f, 0.65f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(1700f, 220f);
        titleRect.anchoredPosition = Vector2.zero;
        titleText = titleGO.AddComponent<Text>();
        titleText.text = "Thank You.";
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.font = fontResource;
        titleText.fontSize = 120;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = AccentYellow;
        titleText.raycastTarget = false;

        // Team-member name (changes per beat)
        var nameGO = new GameObject("Names");
        nameGO.transform.SetParent(canvasGO.transform, false);
        var nameRect = nameGO.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 0.4f);
        nameRect.anchorMax = new Vector2(0.5f, 0.4f);
        nameRect.pivot = new Vector2(0.5f, 0.5f);
        nameRect.sizeDelta = new Vector2(1500f, 140f);
        nameRect.anchoredPosition = Vector2.zero;
        nameText = nameGO.AddComponent<Text>();
        nameText.text = "";
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.font = fontResource;
        nameText.fontSize = 64;
        nameText.color = NameColor;
        nameText.raycastTarget = false;
    }

    IEnumerator PlayCredits()
    {
        // Fade in black background
        yield return Fade(bg, 0f, 1f, BackgroundFadeDuration);

        // Fade in "Thank You."
        yield return Fade(titleText, 0f, 1f, TitleFadeDuration);
        yield return WaitRealtime(TitleHold);

        // Each member: fade in → hold → fade out
        for (int i = 0; i < TeamMembers.Length; i++)
        {
            nameText.text = TeamMembers[i];
            yield return Fade(nameText, 0f, 1f, NameFadeIn);
            yield return WaitRealtime(NameHold);
            yield return Fade(nameText, 1f, 0f, NameFadeOut);
        }

        // Brief pause, then fade title out, then end
        yield return WaitRealtime(0.4f);
        yield return Fade(titleText, 1f, 0f, TitleFadeOutDuration);
        yield return WaitRealtime(FinalHoldBeforeMenu);

        OnFinished?.Invoke();
        Destroy(gameObject);
    }

    static IEnumerator Fade(Graphic g, float fromAlpha, float toAlpha, float duration)
    {
        if (g == null || duration <= 0f) yield break;
        float t = 0f;
        Color c = g.color;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(t / duration));
            g.color = c;
            yield return null;
        }
        c.a = toAlpha;
        g.color = c;
    }

    static IEnumerator WaitRealtime(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
