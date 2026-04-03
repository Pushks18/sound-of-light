using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    void Start()
    {
        var playButton = GameObject.Find("PlayButton");
        if (playButton == null) return;

        var parent = playButton.transform.parent;

        // "Endless (Beta)" button — below Let's Roll
        AddButton(playButton, parent, "EndlessButton", "Endless (Beta)", -330f, LoadEndless);

        // "Room Gen" button — below Endless
        AddButton(playButton, parent, "RoomGenButton", "Room Gen", -480f, LoadRoomGen);
    }

    void AddButton(GameObject template, Transform parent, string name, string label, float yOffset, UnityEngine.Events.UnityAction action)
    {
        var btn = Instantiate(template, parent);
        btn.name = name;

        btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, yOffset);

        var tmp = btn.GetComponentInChildren<TMP_Text>();
        if (tmp != null) tmp.text = label;

        var b = btn.GetComponent<Button>();
        if (b != null)
        {
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(action);
        }
    }

    public void LoadTutorial()
    {
        SceneManager.LoadScene("NewTut");
    }

    public void LoadGame()
    {
        //Debug.Log("Play clicked");
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
}