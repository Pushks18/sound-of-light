using System.Collections;
using TMPro;
using UnityEngine;

public class TextTrigger : MonoBehaviour
{
    public string textMessage;

    [SerializeField] private TextMeshProUGUI textWords;
    private Coroutine hideRoutine;
    private bool hasTriggered = false;
    private Collider2D triggerCollider;

    private void Start()
    {
        triggerCollider = GetComponent<Collider2D>();

        if (textWords == null)
        {
            Debug.LogError("textWords is NOT assigned in Inspector!");
            return;
        }

        textWords.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered)
        {
            return;
        }
        if (!other.CompareTag("Player"))
        {
            return;
        }
        if (textWords == null)
        {
            return;
        }

        hasTriggered = true;

        if (triggerCollider != null)
            triggerCollider.enabled = false;

        textWords.text = textMessage;
        textWords.gameObject.SetActive(true);
        textWords.enabled = true;

        Color c = textWords.color;
        c.a = 1f;
        textWords.color = c;

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(5f);

        if (textWords != null)
        {
            textWords.gameObject.SetActive(false);
        }
    }
}