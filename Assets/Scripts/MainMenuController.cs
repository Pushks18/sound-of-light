using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject mainScreen;
    [SerializeField] private GameObject levelScreen;

    [SerializeField] private Button[] levelButtons;

    public static bool goToLevelOnLoad = false;
    public static int highestLevel = 0;

    void Start()
    {
        var playButton = GameObject.Find("PlayButton");
        if (playButton == null) return;

        if (goToLevelOnLoad)
        {
            GoToLevelScreen();
            goToLevelOnLoad = false;
        }
    }

    public void LoadTutorial()
    {
        SceneManager.LoadScene("NewTut");
    }

    public void LoadGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void LoadEndless()
    {
        SceneManager.LoadScene("ProgressiveRoomGen");
    }

    public void LoadRoomGen()
    {
        SceneManager.LoadScene("RoomGenScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void GoToLevelScreen()
    {
        mainScreen.SetActive(false);
        levelScreen.SetActive(true);

        UpdateLevelButtons();
    }

    public void GoToMainScreen()
    {
        mainScreen.SetActive(true);
        levelScreen.SetActive(false);
    }

    private void UpdateLevelButtons()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelNumber = i + 1; // Level1 = index 0

            if (levelButtons[i] != null)
            {
                levelButtons[i].interactable = levelNumber < highestLevel + 2;
            }
        }
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