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
    private TextMeshProUGUI checkpointLabel;

    private Transform checkpointButtonContainer;
    private bool isPaused;
    private LevelExit cachedExit;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildUI();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.gameEnded)
            return;

        if (cachedExit == null)
            cachedExit = FindAnyObjectByType<LevelExit>();
        if (cachedExit != null && cachedExit.GetLevelDone())
            return;

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
        RefreshCheckpointButtons();
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

        if (player != null)
        {
            var hp = player.GetComponent<PlayerHealth>();
            if (hp != null)
                hpText.text = $"Health: {hp.currentHealth} / {hp.maxHealth}";
            else
                hpText.text = "Health: --";
        }

        if (DungeonManager.Instance != null)
            levelText.text = $"Room: {DungeonManager.Instance.CurrentRoomIndex}";
        else
            levelText.text = "Room: 1";

        int enemies = GameManager.Instance != null ? GameManager.Instance.enemyCount : 0;
        enemiesText.text = $"Enemies Left: {enemies}";
    }

    void RefreshCheckpointButtons()
    {
        ClearChildren(checkpointButtonContainer);

        RoomsManager roomsManager = FindAnyObjectByType<RoomsManager>();
        bool hasRoomsManager = roomsManager != null;

        if (checkpointLabel != null)
            checkpointLabel.gameObject.SetActive(hasRoomsManager);

        if (checkpointButtonContainer != null)
            checkpointButtonContainer.gameObject.SetActive(hasRoomsManager);

        if (!hasRoomsManager)
            return;

        Vector3?[] checkpointPositions = roomsManager.GetCheckpointPositions();
        if (checkpointPositions == null || checkpointPositions.Length <= 1)
            return;

        for (int i = 1; i < checkpointPositions.Length; i++)
        {
            int checkpointNumber = i;
            Vector3? targetPosition = checkpointPositions[i];

            GameObject btnObj = new GameObject($"CheckpointBtn_{checkpointNumber}");
            btnObj.transform.SetParent(checkpointButtonContainer, false);

            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(52f, 52f);

            var btnImage = btnObj.AddComponent<Image>();
            btnImage.color = targetPosition.HasValue
                ? new Color(0.2f, 0.2f, 0.2f, 0.95f)
                : new Color(0.12f, 0.12f, 0.12f, 0.75f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImage;
            btn.interactable = targetPosition.HasValue;

            if (targetPosition.HasValue)
            {
                Vector3 pos = targetPosition.Value;
                btn.onClick.AddListener(() =>
                {
                    TeleportPlayer(pos);
                    Resume();
                });
            }

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);

            var txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            var txt = txtObj.AddComponent<TextMeshProUGUI>();

            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont == null)
                defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (defaultFont == null)
                defaultFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault();

            if (defaultFont != null)
                txt.font = defaultFont;

            txt.text = checkpointNumber.ToString();
            txt.fontSize = 24;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = targetPosition.HasValue ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        }
    }

    void TeleportPlayer(Vector3 targetPosition)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("TeleportPlayer: no player found.");
            return;
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = targetPosition;
            rb.Sleep();
        }

        player.transform.position = new Vector3(
            targetPosition.x,
            targetPosition.y,
            player.transform.position.z
        );

        Debug.Log($"Teleported player to {targetPosition}");
    }

    void ClearChildren(Transform parent)
    {
        if (parent == null) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    void BuildUI()
    {
        var canvasObj = new GameObject("PauseCanvas");
        canvasObj.transform.SetParent(transform, false);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

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

        CreateText(panel.transform, "PauseTitle", "PAUSED",
            64, Color.white, new Vector2(0.5f, 0.72f), defaultFont);

        hpText = CreateText(panel.transform, "PauseHP", "Health: -- / --",
            36, new Color(0.35f, 1f, 0.45f), new Vector2(0.5f, 0.58f), defaultFont);

        levelText = CreateText(panel.transform, "PauseLevel", "Room: 1",
            36, new Color(0.4f, 0.9f, 1f), new Vector2(0.5f, 0.51f), defaultFont);

        enemiesText = CreateText(panel.transform, "PauseEnemies", "Enemies Left: 0",
            36, new Color(1f, 0.85f, 0.2f), new Vector2(0.5f, 0.44f), defaultFont);

        checkpointLabel = CreateText(panel.transform, "CheckpointLabel", "Checkpoint Travel",
            24, new Color(0.8f, 0.8f, 0.8f), new Vector2(0.5f, 0.28f), defaultFont);
        checkpointLabel.gameObject.SetActive(false);

        var containerObj = new GameObject("CheckpointButtons");
        containerObj.transform.SetParent(panel.transform, false);
        var containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.22f);
        containerRect.anchorMax = new Vector2(0.5f, 0.22f);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = new Vector2(700f, 60f);

        var layout = containerObj.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 10f;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.childControlHeight = false;
        layout.childControlWidth = false;

        checkpointButtonContainer = containerObj.transform;
        checkpointButtonContainer.gameObject.SetActive(false);

        var btnObj = new GameObject("MainMenuBtn");
        btnObj.transform.SetParent(panel.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.12f);
        btnRect.anchorMax = new Vector2(0.5f, 0.12f);
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

        CreateText(panel.transform, "PausePrompt", "Press Escape to continue",
            24, new Color(0.7f, 0.7f, 0.7f), new Vector2(0.5f, 0.05f), defaultFont);

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
        Debug.Log("Returning to Main Menu...");
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}