using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelExit : MonoBehaviour
{
    [SerializeField] private GameObject exitScreen;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private string nextLevelSceneName;
    public static int curLevel;
    private bool levelDone = false;

    public bool GetLevelDone()
    {
        return levelDone;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // In demo mode, the demo manager dictates which scene loads next.
            if (DemoSequenceManager.IsActive)
            {
                DemoSequenceManager.Instance.Advance();
                return;
            }
            SceneManager.LoadScene(nextLevelSceneName);
        }
    }

    /*private void OnTriggerEnter2D(Collider2D other)
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

    public void LoadMainMenu()
    {
        MainMenuController.goToLevelOnLoad = true;
        MainMenuController.highestLevel = Mathf.Max(MainMenuController.highestLevel, curLevel);
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadLevel1()
    {
        LoadLevel(1, "Level1");
    }

    public void LoadLevel2()
    {
        LoadLevel(2, "Level2");
    }

    public void LoadLevel3()
    {
        LoadLevel(3, "Level3");
    }

    public void LoadLevel4()
    {
        LoadLevel(4, "Level4");
    }

    public void LoadLevel5()
    {
        LoadLevel(5, "Level5");
    }

    public void LoadLevel6()
    {
        LoadLevel(6, "Level6");
    }

    public void LoadLevel7()
    {
        LoadLevel(7, "Level7");
    }

    public void LoadLevel8()
    {
        LoadLevel(8, "Level8");
    }

    public void LoadLevel9()
    {
        LoadLevel(9, "Level9");
    }

    private void LoadLevel(int nextLevel, string sceneName)
    {
        RunKillAnalytics.Instance?.RecordCurrentLevelCleared();
        LevelExit.curLevel = nextLevel;
        SceneManager.LoadScene(sceneName);
    }*/
}
