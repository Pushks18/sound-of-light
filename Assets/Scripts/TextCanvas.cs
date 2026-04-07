using UnityEngine;
using TMPro;
using System.Collections;

public class TextCanvas : MonoBehaviour
{
    [Header("Title (separate system)")]
    [SerializeField] private TextMeshProUGUI textTitle;

    [Header("Trigger Messages")]
    [SerializeField] private TextMeshProUGUI textWords;

    private Coroutine hideWordsRoutine;

    private void Start()
    {
        if (textTitle != null)
        {
            StartCoroutine(HideTitleAfterDelay());
        }
    }

    private IEnumerator HideTitleAfterDelay()
    {
        yield return new WaitForSeconds(3f);

        if (textTitle != null)
        {
            textTitle.gameObject.SetActive(false);
        }
    }

    public void ShowTriggerText(string message, float duration = 5f)
    {
        if (textWords == null)
        {
            Debug.LogError("TextWords is not assigned in TextCanvas.");
            return;
        }

        textWords.text = message;
        textWords.gameObject.SetActive(true);
        textWords.enabled = true;

        Color c = textWords.color;
        c.a = 1f;
        textWords.color = c;

        // Restart timer if already running
        if (hideWordsRoutine != null)
        {
            StopCoroutine(hideWordsRoutine);
        }

        hideWordsRoutine = StartCoroutine(HordsAfterDelay(duration));
    }

    private IEnumerator HordsAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (textWords != null)
        {
            textWords.gameObject.SetActive(false);
        }

        hideWordsRoutine = null;
    }
}