using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    private GameObject panel;
    private TextMeshProUGUI hpText;
    private TextMeshProUGUI levelText;
    private TextMeshProUGUI enemiesText;
    private bool isPaused;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        // Don't allow pause if game has ended (death/victory)
        if (GameManager.Instance != null && GameManager.Instance.gameEnded) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    void Pause()
    {
        isPaused = true;
        RefreshStats();
        panel.SetActive(true);
        Time.timeScale = 0f;
    }

    void Resume()
    {
        isPaused = false;
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    void RefreshStats()
    {
        var player = GameObject.FindGameObjectWithTag("Player");

        // HP
        if (player != null)
        {
            var hp = player.GetComponent<PlayerHealth>();
            if (hp != null)
                hpText.text = $"Health: {hp.currentHealth} / {hp.maxHealth}";
            else
                hpText.text = "Health: --";
        }

        // Level / Room
        if (DungeonManager.Instance != null)
            levelText.text = $"Room: {DungeonManager.Instance.CurrentRoomIndex}";
        else
            levelText.text = "Room: 1";

        // Enemies
        int enemies = GameManager.Instance != null ? GameManager.Instance.enemyCount : 0;
        enemiesText.text = $"Enemies Left: {enemies}";
    }

    void BuildUI()
    {
        // Canvas
        var canvasObj = new GameObject("PauseCanvas");
        canvasObj.transform.SetParent(transform, false);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Panel (dark overlay)
        panel = new GameObject("PausePanel");
        panel.transform.SetParent(canvasObj.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.85f);

        var defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null)
            defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (defaultFont == null)
            defaultFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault();

        // Title: "PAUSED"
        var title = CreateText(panel.transform, "PauseTitle", "PAUSED",
            64, Color.white, new Vector2(0.5f, 0.7f), defaultFont);

        // Stats
        hpText = CreateText(panel.transform, "PauseHP", "Health: -- / --",
            36, new Color(0.35f, 1f, 0.45f), new Vector2(0.5f, 0.55f), defaultFont);

        levelText = CreateText(panel.transform, "PauseLevel", "Room: 1",
            36, new Color(0.4f, 0.9f, 1f), new Vector2(0.5f, 0.48f), defaultFont);

        enemiesText = CreateText(panel.transform, "PauseEnemies", "Enemies Left: 0",
            36, new Color(1f, 0.85f, 0.2f), new Vector2(0.5f, 0.41f), defaultFont);

        // Controls
        CreateText(panel.transform, "PauseControls",
            "WASD Move  |  Shift Dash  |  J Slash  |  K Shoot  |  L Flash",
            20, new Color(0.6f, 0.6f, 0.6f), new Vector2(0.5f, 0.33f), defaultFont);

        // Main Menu button
        var btnObj = new GameObject("MainMenuBtn");
        btnObj.transform.SetParent(panel.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.25f);
        btnRect.anchorMax = new Vector2(0.5f, 0.25f);
        btnRect.anchoredPosition = Vector2.zero;
        btnRect.sizeDelta = new Vector2(300f, 60f);

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.onClick.AddListener(GoToMainMenu);

        var btnTextObj = new GameObject("BtnText");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        var btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;
        var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) btnText.font = defaultFont;
        btnText.text = "Main Menu";
        btnText.fontSize = 32;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;

        // "Press Escape to continue" prompt
        CreateText(panel.transform, "PausePrompt", "Press Escape to continue",
            24, new Color(0.7f, 0.7f, 0.7f), new Vector2(0.5f, 0.15f), defaultFont);

        panel.SetActive(false);
    }

    TextMeshProUGUI CreateText(Transform parent, string name, string content,
        float fontSize, Color color, Vector2 anchor, TMP_FontAsset font)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(600f, 60f);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    void GoToMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
