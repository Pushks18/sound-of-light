using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public TMP_Text endText;
    public GameObject gameplayUI;

    public int enemyCount;
    public bool gameEnded { get; private set; } = false;
    private bool playerWon = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Clear static event to prevent stale delegates from previous scene
        EnemyHealth.OnEnemyKilled = null;

        // Count enemies immediately
        enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

        // We delay UI update to next frame to ensure GameUIManager exists
        StartCoroutine(InitializeUI());

        if (endText != null)
            endText.gameObject.SetActive(false);
    }

    System.Collections.IEnumerator InitializeUI()
    {
        yield return null; // wait one frame
        GameUIManager.Instance?.UpdateEnemyCount(enemyCount);
        StatusHUD.Instance?.UpdateEnemies(enemyCount);
    }

    void Update()
    {
        if (playerWon && Input.GetKeyDown(KeyCode.Space))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void EnemyKilled()
    {
        Debug.Log("EnemyKilled() called");

        enemyCount--;

        GameUIManager.Instance?.UpdateEnemyCount(enemyCount);
        StatusHUD.Instance?.UpdateEnemies(enemyCount);

        if (enemyCount <= 0)
        {
            PlayerWon();
        }
    }

    public void PlayerDied()
    {
        if (gameEnded) return;

        // Just mark game as ended. The DeathScreen handles the death UI,
        // pause, and scene restart.
        gameEnded = true;
    }

    public void PlayerWon()
    {
        if (gameEnded) return;

        gameEnded = true;
        playerWon = true;
        Time.timeScale = 0f;

        // Use assigned endText if available, otherwise build win UI dynamically
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
    }

    void BuildWinScreen()
    {
        // Find or create a Canvas
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasObj = new GameObject("WinCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

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
    }
}
