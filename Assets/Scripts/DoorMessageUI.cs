using UnityEngine;
using TMPro;
using System.Collections;

public class DoorMessageUI : MonoBehaviour
{
    private static DoorMessageUI instance;
    private TMP_Text text;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        text = GetComponent<TMP_Text>();
        gameObject.SetActive(false);
    }

    public static void Show(string message, float duration = 2f)
    {
        if (instance == null) return;

        // Must activate the GameObject before StartCoroutine (coroutines
        // cannot run on inactive GameObjects).
        instance.gameObject.SetActive(true);
        instance.StopAllCoroutines();
        instance.StartCoroutine(instance.ShowRoutine(message, duration));
    }

    IEnumerator ShowRoutine(string message, float duration)
    {
        text.text = message;
        gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(duration);

        gameObject.SetActive(false);
    }
}