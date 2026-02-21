using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public TMP_Text tutorialText;

    private bool hasShot = false;
    private bool hasMoved = false;
    private bool showingMoveText = false;
    private bool hidingMoveText = false;

    void Start()
    {
        if (tutorialText == null)
        {
            Debug.LogError("TutorialText not assigned!");
            return;
        }

        tutorialText.text = "Left click to shoot";
        tutorialText.gameObject.SetActive(true);
    }

    void Update()
    {
        // Detect first shot
        if (!hasShot && Input.GetMouseButtonDown(0))
        {
            hasShot = true;
            StartCoroutine(HandleShootTutorial());
        }

        // Detect movement AFTER move tutorial is shown
        if (showingMoveText && !hasMoved && !hidingMoveText)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (h != 0 || v != 0)
            {
                hasMoved = true;
                StartCoroutine(HideMoveTutorialAfterDelay());
            }
        }
    }

    IEnumerator HandleShootTutorial()
    {
        // Keep "Left click to shoot" visible
        yield return new WaitForSeconds(2.5f);

        tutorialText.gameObject.SetActive(false);

        // Small pause
        yield return new WaitForSeconds(0.3f);

        // Show movement tutorial
        tutorialText.text = "WASD to move";
        tutorialText.gameObject.SetActive(true);
        showingMoveText = true;
    }

    IEnumerator HideMoveTutorialAfterDelay()
    {
        hidingMoveText = true;

        // Keep "WASD to move" visible after movement
        yield return new WaitForSeconds(2.0f);

        tutorialText.gameObject.SetActive(false);
    }
}