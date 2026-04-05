using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public TMP_Text endText;
    public GameObject gameplayUI;

    public int enemyCount;
    public bool gameEnded { get; private set; } = false;
    private bool playerWon = false;
    private bool huntModeActivated = false;

    [Header("Boss Fight")]
    public bool isBossFight = false;

    [Header("Hunt Mode")]
    [Tooltip("When this many enemies (or fewer) remain, they all activate and pathfind to the player.")]
    public int huntModeThreshold = 3;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Count enemies immediately
        enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

        // We delay UI update to next frame to ensure GameUIManager exists
        StartCoroutine(InitializeUI());

        if (endText != null)
            endText.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    System.Collections.IEnumerator InitializeUI()
    {
        yield return null; // wait one frame
        GameUIManager.Instance?.UpdateEnemyCount(enemyCount);
        StatusHUD.Instance?.UpdateEnemies();
    }

    void Update()
    {
        if (playerWon && (Input.GetKeyDown(KeyCode.Space) ||
                          Input.GetKeyDown(KeyCode.Return) ||
                          Input.GetKeyDown(KeyCode.Escape)))
        {
            ReturnToMainMenu();
        }
    }

    /// <summary>Reset state for a new room in endless mode.</summary>
    public void ResetForNewRoom(int newEnemyCount)
    {
        gameEnded = false;
        huntModeActivated = false;
        enemyCount = newEnemyCount;
        GameUIManager.Instance?.UpdateEnemyCount(enemyCount);
        StatusHUD.Instance?.UpdateEnemies();
    }

    public void EnemyKilled()
    {
        Debug.Log("EnemyKilled() called");

        enemyCount--;

        GameUIManager.Instance?.UpdateEnemyCount(enemyCount);
        StatusHUD.Instance?.UpdateEnemies();

        // Trigger hunt mode once when few enemies remain
        if (!huntModeActivated && enemyCount > 0 && enemyCount <= huntModeThreshold)
        {
            huntModeActivated = true;
            ActivateHuntMode();
        }

        if (enemyCount <= 0)
        {
            // Boss fight victory is handled by BossDefeated(), not enemy count
            if (isBossFight) return;

            // In endless mode, spawn a portal that sucks the player to the next room
            if (DungeonManager.Instance != null)
                RoomClearPortal.Spawn();
            else
                PlayerWon();
        }
    }

    /// <summary>
    /// Called by boss scripts when the boss is defeated.
    /// In progressive mode: continues to the next room (portal + post-boss heal).
    /// In standalone boss scene: triggers the normal victory sequence.
    /// </summary>
    public void BossDefeated()
    {
        if (DungeonManager.IsReturningFromBoss)
        {
            // Progressive run — play the room-clear portal effect then load origin scene.
            // RoomClearPortal handles the scene transition when DungeonManager is absent.
            RoomClearPortal.Spawn();
        }
        else
        {
            // Standalone VesperScene — end the game normally
            PlayerWon();
        }
    }

    void ActivateHuntMode()
    {
        var enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.enabled)
                enemy.ActivateHunt();
        }
        Debug.Log($"[Hunt Mode] {enemies.Length} enemies now hunting the player");
    }

    public void PlayerDied()
    {
        if (gameEnded) return;

        // Just mark game as ended. The DeathScreen handles the death UI,
        // pause, and scene restart.
        gameEnded = true;
    }

    [Header("Victory Light-Up")]
    public float victoryLightDuration = 1f;
    public float victoryLightIntensity = 1f;

    public void PlayerWon()
    {
        if (gameEnded) return;

        gameEnded = true;
        StartCoroutine(VictorySequence());
    }

    IEnumerator VictorySequence()
    {
        Debug.Log("VictorySequence() started");
        // Create a global light that illuminates the entire room
        // Try to find an existing Global Light2D first
        Light2D light = null;
        var lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.lightType == Light2D.LightType.Global)
            {
                light = l;
                break;
            }
        }
        // If none exists create one
        if (light == null)
        {
            var lightObj = new GameObject("VictoryLight");
            light = lightObj.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
        }
        // Now configure it
        light.color = new Color(1f, 0.95f, 0.85f);
        light.intensity = 0f;

        // Fade the light up over the duration
        float elapsed = 0f;
        while (elapsed < victoryLightDuration)
        {
            elapsed += Time.deltaTime;
            light.intensity = Mathf.Lerp(0f, victoryLightIntensity, elapsed / victoryLightDuration);
            yield return null;
        }
        light.intensity = victoryLightIntensity;

        // Now pause and show the win screen
        playerWon = true;
        Time.timeScale = 0f;

        if (endText != null)
        {
            endText.color = Color.green;
            endText.text = "YOU WON!\nPress SPACE to return to Menu";
            endText.gameObject.SetActive(true);
        }
        else
        {
            BuildWinScreen();
        }

        if (gameplayUI != null)
            gameplayUI.SetActive(false);
        
        Debug.Log("ending VictorySequence()");
    }

    void BuildWinScreen()
    {
        Debug.Log("win screen building started");
        // Find or create a Canvas
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasObj = new GameObject("WinCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        EnsureEventSystem();

        // Dark overlay
        var panel = new GameObject("WinPanel");
        panel.transform.SetParent(canvas.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.85f);

        var defaultFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault();

        // "YOU WON!" title
        var titleObj = new GameObject("WinTitle");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.55f);
        titleRect.anchorMax = new Vector2(0.5f, 0.55f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(600f, 100f);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) titleText.font = defaultFont;
        titleText.text = "YOU WON!";
        titleText.fontSize = 72;
        titleText.color = Color.green;
        titleText.alignment = TextAlignmentOptions.Center;

        // Prompt
        var promptObj = new GameObject("WinPrompt");
        promptObj.transform.SetParent(panel.transform, false);
        var promptRect = promptObj.AddComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0.4f);
        promptRect.anchorMax = new Vector2(0.5f, 0.4f);
        promptRect.anchoredPosition = Vector2.zero;
        promptRect.sizeDelta = new Vector2(600f, 60f);
        var promptText = promptObj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) promptText.font = defaultFont;
        promptText.text = "Press Space to return to Menu";
        promptText.fontSize = 32;
        promptText.color = Color.white;
        promptText.alignment = TextAlignmentOptions.Center;

        // Main menu button
        var buttonObj = new GameObject("MainMenuButton");
        buttonObj.transform.SetParent(panel.transform, false);
        var buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.28f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.28f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = new Vector2(280f, 70f);

        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        var button = buttonObj.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = buttonImage.color;
        colors.highlightedColor = new Color(0.22f, 0.22f, 0.22f, 1f);
        colors.pressedColor = new Color(0.10f, 0.10f, 0.10f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(ReturnToMainMenu);

        var buttonTextObj = new GameObject("Text");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        var buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        var buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        if (defaultFont != null) buttonText.font = defaultFont;
        buttonText.text = "Main Menu";
        buttonText.fontSize = 30;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;

        EventSystem.current?.SetSelectedGameObject(buttonObj);
        Debug.Log("win screen building ended");
    }

    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

        var eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();
    }

    void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
