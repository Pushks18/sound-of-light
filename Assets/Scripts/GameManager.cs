using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public TMP_Text endText;
    public GameObject gameplayUI;

    public int enemyCount;
    private bool gameEnded = false;

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

        endText.gameObject.SetActive(false);
    }

    System.Collections.IEnumerator InitializeUI()
    {
        yield return null; // wait one frame
        GameUIManager.Instance?.UpdateEnemyCount(enemyCount);
    }

    void Update()
    {
        if (gameEnded && Input.GetKeyDown(KeyCode.Space))
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

        if (enemyCount <= 0)
        {
            PlayerWon();
        }
    }

    public void PlayerDied()
    {
        if (gameEnded) return;

        gameEnded = true;
        Time.timeScale = 0f;

        endText.color = Color.red;
        endText.text = "YOU DIED\nPress SPACE to return to Menu";
        endText.gameObject.SetActive(true);

        if (gameplayUI != null)
            gameplayUI.SetActive(false);
    }

    public void PlayerWon()
    {
        if (gameEnded) return;

        gameEnded = true;
        Time.timeScale = 0f;

        endText.color = Color.green;
        endText.text = "YOU WON!\nPress SPACE to return to Menu";
        endText.gameObject.SetActive(true);

        if (gameplayUI != null)
            gameplayUI.SetActive(false);
    }
}