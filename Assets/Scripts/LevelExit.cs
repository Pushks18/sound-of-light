using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelExit : MonoBehaviour
{
    [SerializeField] private GameObject exitScreen;
    [SerializeField] private TextMeshProUGUI hpText;
    public static int curLevel;
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
        MainMenuController.highestLevel = Mathf.Max(MainMenuController.highestLevel, curLevel);
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadLevel1()
    {
        LevelExit.curLevel = 1; 
        SceneManager.LoadScene("Level1");
    }

    public void LoadLevel2()
    {
        LevelExit.curLevel = 2; 
        SceneManager.LoadScene("Level2");
    }

    public void LoadLevel3()
    {
        LevelExit.curLevel = 3; 
        SceneManager.LoadScene("Level3");
    }

    public void LoadLevel4()
    {
        LevelExit.curLevel = 4; 
        SceneManager.LoadScene("Level4");
    }

    public void LoadLevel5()
    {
        LevelExit.curLevel = 5; 
        SceneManager.LoadScene("Level5");
    }

    public void LoadLevel6()
    {
        LevelExit.curLevel = 6; 
        SceneManager.LoadScene("Level6");
    }

    public void LoadLevel7()
    {
        LevelExit.curLevel = 7; 
        SceneManager.LoadScene("Level7");
    }

    public void LoadLevel8()
    {
        LevelExit.curLevel = 8; 
        SceneManager.LoadScene("Level8");
    }

    public void LoadLevel9()
    {
        LevelExit.curLevel = 9; 
        SceneManager.LoadScene("Level9");
    }
}
