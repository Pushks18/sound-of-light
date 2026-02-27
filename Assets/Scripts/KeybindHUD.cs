using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds a persistent keybind HUD at the top-center of the screen entirely in code.
/// Attach this script to any GameObject in the scene — no prefab or Inspector setup needed.
/// </summary>
public class KeybindHUD : MonoBehaviour
{
    // Layout
    private const float PanelWidth    = 600f;
    private const float PanelHeight   = 48f;
    private const float TopMargin     = 14f;   // distance from top of screen

    // Styling
    private const float FontSize      = 18f;
    private static readonly Color TextColor     = new Color(1f,   1f,   1f,   0.92f);
    private static readonly Color PanelBgColor  = new Color(0f,   0f,   0f,   0.50f);
    private static readonly Color KeyColor      = new Color(1f,   0.85f, 0.2f, 1f);  // yellow for key labels

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        BuildHUD();
    }

    void BuildHUD()
    {
        // ── Root Canvas (Screen Space Overlay) ───────────────────────────────
        var canvasGO = new GameObject("KeybindHUD_Canvas");
        canvasGO.transform.SetParent(transform);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Semi-transparent pill panel ───────────────────────────────────────
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);

        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = PanelBgColor;
        // Rounded-rect look via sprite — falls back to a plain rect if no sprite
        panelImg.raycastTarget = false;

        var panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 1f);
        panelRT.anchorMax = new Vector2(0.5f, 1f);
        panelRT.pivot     = new Vector2(0.5f, 1f);
        panelRT.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        panelRT.anchoredPosition = new Vector2(0f, -TopMargin);

        // ── Keybind text ──────────────────────────────────────────────────────
        var textGO = new GameObject("KeybindText");
        textGO.transform.SetParent(panelGO.transform, false);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.raycastTarget = false;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.fontSize      = FontSize;

        // Build rich-text keybind string with coloured key labels
        // Format:  [J] Slash   [K] Shoot   [Shift] Dash   [L] Flash
        string kc  = ColorToHex(KeyColor);
        string tc  = ColorToHex(TextColor);
        tmp.text = string.Format(
            "<color=#{0}>[J]</color><color=#{1}> Slash    </color>" +
            "<color=#{0}>[K]</color><color=#{1}> Shoot    </color>" +
            "<color=#{0}>[Shift]</color><color=#{1}> Dash    </color>" +
            "<color=#{0}>[L]</color><color=#{1}> Flash</color>",
            kc, tc);

        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin        = Vector2.zero;
        textRT.anchorMax        = Vector2.one;
        textRT.offsetMin        = new Vector2(8f,  0f);
        textRT.offsetMax        = new Vector2(-8f, 0f);
    }

    // Converts a Color to a 6-char hex string for TMP rich text
    static string ColorToHex(Color c)
    {
        return ColorUtility.ToHtmlStringRGB(c);
    }
}
