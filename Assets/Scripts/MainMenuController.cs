using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject mainScreen;
    [SerializeField] private GameObject levelScreen;
    public static bool goToLevelOnLoad = false;

    void Start()
    {
        var playButton = GameObject.Find("PlayButton");
        if (playButton == null) return;
        var parent = playButton.transform.parent;
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
        SceneManager.LoadScene("RoomGenScene");
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
    }

    public void GoToMainScreen()
    {
        mainScreen.SetActive(true);
        levelScreen.SetActive(false);
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