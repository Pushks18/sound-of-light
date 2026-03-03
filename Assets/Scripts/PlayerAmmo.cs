using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tracks discrete ammo, dash, and flash counts for the player.
/// Builds its own on-screen HUD entirely in code — no prefab or Inspector wiring needed.
/// Add this component to the Player GameObject.
/// </summary>
public class PlayerAmmo : MonoBehaviour
{
    public static PlayerAmmo Instance { get; private set; }

    [Header("Starting Counts")]
    public int maxBullets = 15;
    public int maxDashes  = 5;
    public int maxFlashes = 5;

    // Current counts
    public int Bullets  { get; private set; }
    public int Dashes   { get; private set; }
    public int Flashes  { get; private set; }

    // HUD text refs
    private TextMeshProUGUI bulletLabel;
    private TextMeshProUGUI dashLabel;
    private TextMeshProUGUI flashLabel;

    // Styling
    private static readonly Color ColAvailable = new Color(1f,   1f,   1f,   1f);
    private static readonly Color ColEmpty     = new Color(1f,   0.3f, 0.3f, 0.8f);
    private static readonly Color ColKey       = new Color(1f,   0.85f, 0.2f, 1f);

    // Per-ability regen timers
    [Header("Bullet Regen")]
    public float bulletRegenDelay    = 5f;   // seconds after last shot before regen starts
    public float bulletRegenInterval = 0.5f; // one bullet every 0.5s once regen starts

    [Header("Dash Regen")]
    public float dashRegenDelay    = 8f;     // seconds after last dash
    public float dashRegenInterval = 2f;     // one dash every 2s

    [Header("Flash Regen")]
    public float flashRegenDelay    = 8f;    // seconds after last flash
    public float flashRegenInterval = 2f;    // one flash every 2s

    private float lastBulletUseTime = Mathf.NegativeInfinity;
    private float lastDashUseTime   = Mathf.NegativeInfinity;
    private float lastFlashUseTime  = Mathf.NegativeInfinity;

    private float bulletRegenAccum;
    private float dashRegenAccum;
    private float flashRegenAccum;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        Bullets = maxBullets;
        Dashes  = maxDashes;
        Flashes = maxFlashes;

        BuildHUD();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        // Bullet regen
        if (Bullets < maxBullets && Time.time - lastBulletUseTime >= bulletRegenDelay)
        {
            bulletRegenAccum += Time.deltaTime;
            while (bulletRegenAccum >= bulletRegenInterval && Bullets < maxBullets)
            {
                bulletRegenAccum -= bulletRegenInterval;
                Bullets++;
            }
        }

        // Dash regen
        if (Dashes < maxDashes && Time.time - lastDashUseTime >= dashRegenDelay)
        {
            dashRegenAccum += Time.deltaTime;
            while (dashRegenAccum >= dashRegenInterval && Dashes < maxDashes)
            {
                dashRegenAccum -= dashRegenInterval;
                Dashes++;
            }
        }

        // Flash regen
        if (Flashes < maxFlashes && Time.time - lastFlashUseTime >= flashRegenDelay)
        {
            flashRegenAccum += Time.deltaTime;
            while (flashRegenAccum >= flashRegenInterval && Flashes < maxFlashes)
            {
                flashRegenAccum -= flashRegenInterval;
                Flashes++;
            }
        }

        // Always keep HUD in sync while any resource is regenerating
        if (Bullets < maxBullets || Dashes < maxDashes || Flashes < maxFlashes)
            RefreshHUD();
    }

    public bool TrySpendBullet()
    {
        if (Bullets <= 0) return false;
        Bullets--;
        lastBulletUseTime = Time.time;
        bulletRegenAccum = 0f;
        RefreshHUD();
        return true;
    }

    public bool TrySpendDash()
    {
        if (Dashes <= 0) return false;
        Dashes--;
        lastDashUseTime = Time.time;
        dashRegenAccum = 0f;
        RefreshHUD();
        return true;
    }

    public bool TrySpendFlash()
    {
        if (Flashes <= 0) return false;
        Flashes--;
        lastFlashUseTime = Time.time;
        flashRegenAccum = 0f;
        RefreshHUD();
        return true;
    }

    // ── HUD Builder ──────────────────────────────────────────────────────────

    void BuildHUD()
    {
        // Root canvas — Screen Space Overlay
        var canvasGO = new GameObject("AmmoHUD_Canvas");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Container sitting top-right corner
        var container = new GameObject("AmmoContainer");
        container.transform.SetParent(canvasGO.transform, false);

        var vLayout = container.AddComponent<VerticalLayoutGroup>();
        vLayout.childAlignment        = TextAnchor.UpperRight;
        vLayout.childControlWidth     = true;
        vLayout.childControlHeight    = true;
        vLayout.childForceExpandWidth = false;
        vLayout.spacing               = 4f;
        vLayout.padding               = new RectOffset(12, 12, 10, 10);

        var containerRT = container.GetComponent<RectTransform>();
        containerRT.anchorMin       = new Vector2(1f, 1f);
        containerRT.anchorMax       = new Vector2(1f, 1f);
        containerRT.pivot           = new Vector2(1f, 1f);
        containerRT.anchoredPosition = new Vector2(-16f, -16f);
        containerRT.sizeDelta        = new Vector2(220f, 0f);

        // Semi-transparent background panel
        var bgImg = container.AddComponent<Image>();
        bgImg.color         = new Color(0f, 0f, 0f, 0.52f);
        bgImg.raycastTarget = false;

        var fitter = container.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Build the three rows
        bulletLabel = MakeRow(container.transform, "\U0001F525 [K] Bullets", Bullets, maxBullets);
        dashLabel   = MakeRow(container.transform, "\U0001F4A8 [Shift] Dash", Dashes,  maxDashes);
        flashLabel  = MakeRow(container.transform, "\u26A1 [L] Flash",    Flashes, maxFlashes);
    }

    TextMeshProUGUI MakeRow(Transform parent, string label, int current, int max)
    {
        var rowGO = new GameObject("Row_" + label);
        rowGO.transform.SetParent(parent, false);

        var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment        = TextAnchor.MiddleRight;
        hlg.childControlWidth     = true;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth = true;
        hlg.spacing               = 0f;

        var tmp = rowGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize      = 17f;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.alignment     = TextAlignmentOptions.Right;
        tmp.raycastTarget = false;
        tmp.text          = FormatRow(label, current, max);

        return tmp;
    }

    void RefreshHUD()
    {
        if (bulletLabel != null)
            bulletLabel.text = FormatRow("\U0001F525 [K] Bullets", Bullets, maxBullets);
        if (dashLabel != null)
            dashLabel.text   = FormatRow("\U0001F4A8 [Shift] Dash",  Dashes,  maxDashes);
        if (flashLabel != null)
            flashLabel.text  = FormatRow("\u26A1 [L] Flash",    Flashes, maxFlashes);
    }

    string FormatRow(string label, int current, int max)
    {
        Color c     = current > 0 ? ColAvailable : ColEmpty;
        string hex  = ColorUtility.ToHtmlStringRGB(c);
        string keyHex = ColorUtility.ToHtmlStringRGB(ColKey);

        // Build pip icons:  ■■■□□
        string pips = BuildPips(current, max);

        return $"<color=#{keyHex}>{label}</color>  <color=#{hex}>{pips} {current}/{max}</color>";
    }

    string BuildPips(int current, int max)
    {
        string full  = "<color=#FFE066>\u25A0</color>"; // ■ yellow
        string empty = "<color=#555555>\u25A1</color>"; // □ grey
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < max; i++)
            sb.Append(i < current ? full : empty);
        return sb.ToString();
    }
}
