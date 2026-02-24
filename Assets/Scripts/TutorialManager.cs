using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public TMP_Text tutorialText;

    private int enemiesKilled = 0;
    private bool tutorialFinished = false;

    void OnEnable()
    {
        EnemyHealth.OnEnemyKilled += HandleEnemyKilled;
    }

    void OnDisable()
    {
        EnemyHealth.OnEnemyKilled -= HandleEnemyKilled;
    }

    void Start()
    {
        StartCoroutine(TutorialSequence());
    }

    void Update()
    {
        if (tutorialFinished && Input.GetKeyDown(KeyCode.Space))
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    IEnumerator TutorialSequence()
{
    // STEP 1 — Move
    tutorialText.text = "WASD to move";
    tutorialText.gameObject.SetActive(true);

    yield return new WaitUntil(() => PlayerMoved());

    tutorialText.gameObject.SetActive(false);
    yield return new WaitForSeconds(0.5f);

    // STEP 2 — Slash
    tutorialText.text = "Press J to Slash";
    tutorialText.gameObject.SetActive(true);

    yield return new WaitUntil(() => enemiesKilled >= 1);

    tutorialText.text = "Avoid the trap";
    yield return new WaitForSeconds(8f);

    // STEP 3 — Shoot
    enemiesKilled = 0;
    tutorialText.text = "Press K to Shoot";

    yield return new WaitUntil(() => enemiesKilled >= 1);

    tutorialText.gameObject.SetActive(false);
    yield return new WaitForSeconds(0.5f);

    // STEP 4 — Flash
    tutorialText.text = "Press L to Flash";
    tutorialText.gameObject.SetActive(true);

    yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.L));

    yield return new WaitForSeconds(2f);

    tutorialText.text = "Tutorial Complete\nPress SPACE to return to Menu";
    tutorialFinished = true;
}

    void HandleEnemyKilled()
    {
        enemiesKilled++;
    }

    bool PlayerMoved()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        return h != 0 || v != 0;
    }
}