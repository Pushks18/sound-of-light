using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelExit : MonoBehaviour
{
    [SerializeField] private GameObject exitScreen;
    [SerializeField] private TextMeshProUGUI hpText;
    private bool levelDone = false;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            levelDone = true;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var hp = player.GetComponent<PlayerHealth>();
                if (hp != null)
                    hpText.text = $"Health: {hp.currentHealth} / {hp.maxHealth}";
                else
                    hpText.text = "Health: --";
            }
            exitScreen.SetActive(true);
        }
    }

    public bool GetLevelDone()
    {
        return levelDone;
    }

    public void LoadMainMenu()
    {
        MainMenuController.goToLevelOnLoad = true;
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadLevel1()
    {
        SceneManager.LoadScene("Level1");
    }

    public void LoadLevel2()
    {
        SceneManager.LoadScene("Level2");
    }

    public void LoadLevel3()
    {
        SceneManager.LoadScene("Level3");
    }
}
