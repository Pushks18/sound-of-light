using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class DeathScreen : MonoBehaviour
{
    private GameObject panel;
    private bool isDead;

    void Start()
    {
        // Build death screen UI under this Canvas
        panel = new GameObject("DeathPanel");
        panel.transform.SetParent(transform, false);

        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.85f);

        // "YOU DIED" title
        var titleObj = new GameObject("DeathTitle");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.55f);
        titleRect.anchorMax = new Vector2(0.5f, 0.55f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(600f, 100f);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "YOU DIED";
        titleText.fontSize = 72;
        titleText.color = new Color(0.9f, 0.2f, 0.2f);
        titleText.alignment = TextAlignmentOptions.Center;

        // "Press Space to play again" prompt
        var promptObj = new GameObject("DeathPrompt");
        promptObj.transform.SetParent(panel.transform, false);
        var promptRect = promptObj.AddComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0.4f);
        promptRect.anchorMax = new Vector2(0.5f, 0.4f);
        promptRect.anchoredPosition = Vector2.zero;
        promptRect.sizeDelta = new Vector2(600f, 60f);
        var promptText = promptObj.AddComponent<TextMeshProUGUI>();
        promptText.text = "Press Space to play again";
        promptText.fontSize = 32;
        promptText.color = Color.white;
        promptText.alignment = TextAlignmentOptions.Center;

        panel.SetActive(false);
    }

    void Update()
    {
        if (isDead && Input.GetKeyDown(KeyCode.Space))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void Show()
    {
        isDead = true;
        panel.SetActive(true);
        Time.timeScale = 0f;
    }
}
