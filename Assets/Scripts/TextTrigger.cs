using UnityEngine;

public class TextTrigger : MonoBehaviour
{
    public string textMessage;
    [SerializeField] private int textNumber;
    [SerializeField] private KeyCode[] dismissKeys;

    [SerializeField] private TextCanvas textCanvas;

    private bool hasTriggered = false;
    private Collider2D triggerCollider;

    private void Start()
    {
        triggerCollider = GetComponent<Collider2D>();

        if (textCanvas == null)
        {
            Debug.LogError("TextCanvas is not assigned in Inspector.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;

        if (triggerCollider != null)
            triggerCollider.enabled = false;

        if (textCanvas != null)
        {
            textCanvas.ShowTriggerText(textMessage, textNumber, dismissKeys);
        }
    }
}