using UnityEngine;
using TMPro;
using System.Collections;

public class DoorMessageUI : MonoBehaviour
{
    private static DoorMessageUI instance;
    private TMP_Text text;

    void Awake()
    {
        instance = this;
        text = GetComponent<TMP_Text>();
        gameObject.SetActive(false);
    }

    public static void Show(string message, float duration = 2f)
    {
        if (instance == null) return;

        instance.StopAllCoroutines();
        instance.StartCoroutine(instance.ShowRoutine(message, duration));
    }

    IEnumerator ShowRoutine(string message, float duration)
    {
        text.text = message;
        gameObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        gameObject.SetActive(false);
    }
}