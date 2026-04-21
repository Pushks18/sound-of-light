using UnityEngine;
using TMPro;
using System.Collections;

public class TextCanvas : MonoBehaviour
{
    [Header("Title (separate system)")]
    [SerializeField] private TextMeshProUGUI textTitle;

    [Header("Trigger Messages")]
    [SerializeField] private TextMeshProUGUI textWords;

    private Coroutine hideTitleRoutine;

    private string currentMessage = "";
    private int currentTextNumber = -1;
    private KeyCode[] currentDismissKeys;

    private RoomsManager roomsManager;

    private void Awake()
    {
        roomsManager = FindAnyObjectByType<RoomsManager>();
    }

    private void Start()
    {
        if (textTitle != null)
        {
            hideTitleRoutine = StartCoroutine(HideTitleAfterDelay());
        }
    }

    private void Update()
    {
        if (textWords == null || !textWords.gameObject.activeSelf)
            return;

        if (currentDismissKeys == null || currentDismissKeys.Length == 0)
            return;

        foreach (KeyCode key in currentDismissKeys)
        {
            if (Input.GetKeyDown(key))
            {
                HideCurrentTextAndDisableDoor();
                break;
            }
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

    public void ShowTriggerText(string message, int textNumber, KeyCode[] dismissKeys)
    {
        if (textWords == null)
        {
            Debug.LogError("TextWords is not assigned in TextCanvas.");
            return;
        }

        currentMessage = message;
        currentTextNumber = textNumber;
        currentDismissKeys = dismissKeys;

        textWords.text = message;
        textWords.gameObject.SetActive(true);
        textWords.enabled = true;

        Color c = textWords.color;
        c.a = 1f;
        textWords.color = c;
    }

    private void HideCurrentTextAndDisableDoor()
    {
        if (textWords != null)
        {
            textWords.gameObject.SetActive(false);
        }

        if (roomsManager == null)
        {
            roomsManager = FindAnyObjectByType<RoomsManager>();
        }

        if (roomsManager != null && currentTextNumber >= 0)
        {
            roomsManager.DisableDoor(currentTextNumber);
        }

        currentMessage = "";
        currentTextNumber = -1;
        currentDismissKeys = null;
    }
}