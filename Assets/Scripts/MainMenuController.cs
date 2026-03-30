using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    void Start()
    {
        // Dynamically add "Endless (Beta)" button below PlayButton
        var playButton = GameObject.Find("PlayButton");
        if (playButton != null)
        {
            var endless = Instantiate(playButton, playButton.transform.parent);
            endless.name = "EndlessButton";

            var rt = endless.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, -330f);

            var tmp = endless.GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.text = "Endless (Beta)";

            var btn = endless.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(LoadEndless);
            }
        }
    }

    public void LoadTutorial()
    {
        SceneManager.LoadScene("TutorialScene");
    }

    public void LoadGame()
    {
        //Debug.Log("Play clicked");
        SceneManager.LoadScene("GameScene");
    }

    public void LoadEndless()
    {
        //Debug.Log("Endless clicked");
        SceneManager.LoadScene("ProgressiveRoomGen");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}