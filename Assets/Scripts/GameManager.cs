using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public TMP_Text endText;
    public GameObject gameplayUI; // optional UI to hide

    private bool gameEnded = false;

    void Awake()
    {
        Instance = this;
        endText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (gameEnded && Input.GetKeyDown(KeyCode.R))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void PlayerDied()
    {
        if (gameEnded) return;

        gameEnded = true;
        Time.timeScale = 0f;

        endText.color = Color.red;
        endText.text = "YOU DIED\nPress R to Restart";
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
        endText.text = "YOU WIN\nPress R to Restart";
        endText.gameObject.SetActive(true);

        if (gameplayUI != null)
            gameplayUI.SetActive(false);
    }
}