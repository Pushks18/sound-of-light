using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds a self-contained top-left HUD showing HP, Flash cooldown, and Enemies remaining.
/// Attach this script to the Player (or any persistent GameObject) — no prefab or Inspector needed.
/// Call StatusHUD.Instance.UpdateX() methods from wherever the values change.
/// </summary>
public class StatusHUD : MonoBehaviour
{
    public static StatusHUD Instance { get; private set; }

    // ── Cached state ─────────────────────────────────────────────────────────
    private int   currentHP      = 5;
    private int   maxHP          = 5;
    private float flashCooldown  = 0f;      // seconds remaining on cooldown
    private float flashMaxCD     = 20f;     // full cooldown duration
    private int   enemiesLeft    = 0;

    // ── HUD text refs ────────────────────────────────────────────────────────
    private TextMeshProUGUI hpLabel;
    private TextMeshProUGUI flashLabel;
    private TextMeshProUGUI enemyLabel;

    // ── Colours ──────────────────────────────────────────────────────────────
    private static readonly Color ColGreen  = new Color(0.35f, 1f,   0.45f, 1f);   // healthy HP
    private static readonly Color ColYellow = new Color(1f,   0.85f, 0.2f,  1f);   // mid HP
    private static readonly Color ColRed    = new Color(1f,   0.25f, 0.2f,  1f);   // low HP / cooldown
    private static readonly Color ColReady  = new Color(0.4f, 0.9f,  1f,   1f);    // flash ready

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildHUD();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        // Tick down flash cooldown every frame and refresh the label
        if (flashCooldown > 0f)
        {
            flashCooldown -= Time.deltaTime;
            if (flashCooldown < 0f) flashCooldown = 0f;
            RefreshFlash();
        }
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void UpdateHP(int current, int max = -1)
    {
        currentHP = current;
        if (max > 0) maxHP = max;
        RefreshHP();
    }

    public void StartFlashCooldown(float cooldownDuration)
    {
        flashCooldown = cooldownDuration;
        flashMaxCD    = cooldownDuration;
        RefreshFlash();
    }

    public void UpdateEnemies(int count)
    {
        enemiesLeft = count;
        RefreshEnemy();
    }

    // ── HUD Builder ──────────────────────────────────────────────────────────

    void BuildHUD()
    {
        // Root Canvas
        var canvasGO = new GameObject("StatusHUD_Canvas");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Container — top-left corner
        var container = new GameObject("StatusContainer");
        container.transform.SetParent(canvasGO.transform, false);

        var vLayout = container.AddComponent<VerticalLayoutGroup>();
        vLayout.childAlignment        = TextAnchor.UpperLeft;
        vLayout.childControlWidth     = true;
        vLayout.childControlHeight    = true;
        vLayout.childForceExpandWidth = false;
        vLayout.spacing               = 4f;
        vLayout.padding               = new RectOffset(12, 12, 10, 10);

        var ct = container.GetComponent<RectTransform>();
        ct.anchorMin        = new Vector2(0f, 1f);
        ct.anchorMax        = new Vector2(0f, 1f);
        ct.pivot            = new Vector2(0f, 1f);
        ct.anchoredPosition = new Vector2(16f, -16f);
        ct.sizeDelta        = new Vector2(260f, 0f);

        // Semi-transparent background
        var bg = container.AddComponent<Image>();
        bg.color         = new Color(0f, 0f, 0f, 0.52f);
        bg.raycastTarget = false;

        var fitter = container.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Build rows
        hpLabel    = MakeRow(container.transform, "\u2665 HP");
        flashLabel = MakeRow(container.transform, "\u26A1 Flash");
        enemyLabel = MakeRow(container.transform, "\u2620 Enemies");

        // Initialise display
        RefreshHP();
        RefreshFlash();
        RefreshEnemy();
    }

    TextMeshProUGUI MakeRow(Transform parent, string rowName)
    {
        var go = new GameObject("Row_" + rowName);
        go.transform.SetParent(parent, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize      = 17f;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.alignment     = TextAlignmentOptions.Left;
        tmp.raycastTarget = false;

        return tmp;
    }

    // ── Refresh helpers ───────────────────────────────────────────────────────

    void RefreshHP()
    {
        if (hpLabel == null) return;

        Color c = currentHP >= maxHP         ? ColGreen
                : currentHP > maxHP / 2      ? ColYellow
                :                              ColRed;

        string hex  = ColorUtility.ToHtmlStringRGB(c);
        string pips = BuildPips(currentHP, maxHP, "\u2665", "\u2661");   // ♥ / ♡

        hpLabel.text = $"\u2665 <b>HP</b>  <color=#{hex}>{pips}  {currentHP}/{maxHP}</color>";
    }

    void RefreshFlash()
    {
        if (flashLabel == null) return;

        if (flashCooldown <= 0f)
        {
            string readyHex = ColorUtility.ToHtmlStringRGB(ColReady);
            flashLabel.text = $"\u26A1 <b>Flash</b>  <color=#{readyHex}>READY</color>";
        }
        else
        {
            string cdHex = ColorUtility.ToHtmlStringRGB(ColRed);
            // Progress bar: 10 segments
            int filled = Mathf.RoundToInt((1f - flashCooldown / flashMaxCD) * 10f);
            string bar = BuildPips(filled, 10, "\u25A0", "\u25A1");
            flashLabel.text = $"\u26A1 <b>Flash</b>  <color=#{cdHex}>{bar} {flashCooldown:F1}s</color>";
        }
    }

    void RefreshEnemy()
    {
        if (enemyLabel == null) return;

        Color c   = enemiesLeft <= 0  ? ColGreen : ColYellow;
        string hex = ColorUtility.ToHtmlStringRGB(c);
        string txt = enemiesLeft <= 0 ? "All clear!" : enemiesLeft.ToString();
        enemyLabel.text = $"\u2620 <b>Enemies</b>  <color=#{hex}>{txt}</color>";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    string BuildPips(int current, int max, string full, string empty)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < max; i++)
            sb.Append(i < current ? full : empty);
        return sb.ToString();
    }
}
