using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void LoadTutorial()
    {
        SceneManager.LoadScene("TutorialScene");
    }
public void LoadGame()
{
    Debug.Log("Play clicked");
    SceneManager.LoadScene("BaseScene");
}
    // public void LoadGame()
    // {
        
    //     SceneManager.LoadScene("GameScene");
    // }

    public void QuitGame()
    {
        Application.Quit();
    }
}