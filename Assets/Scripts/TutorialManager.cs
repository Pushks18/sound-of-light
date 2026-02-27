using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public TMP_Text tutorialText;

    [Header("Global Light")]
    [Tooltip("Drag the Global Light 2D GameObject from the hierarchy here.")]
    public Light2D globalLight;

    private const float LIGHT_BRIGHT   = 1f;   // normal tutorial brightness
    private const float LIGHT_DARK     = 0.02f;  // after dimming

    private int  enemiesKilled   = 0;
    private bool tutorialFinished = false;
    private bool playerDashed    = false;
    private bool playerShot      = false;

    private PlayerMovement playerMovement;

    // ─────────────────────────────────────────────────────────────────────────
    void OnEnable()  => EnemyHealth.OnEnemyKilled += HandleEnemyKilled;
    void OnDisable()
    {
        EnemyHealth.OnEnemyKilled -= HandleEnemyKilled;
        if (playerMovement != null)
            playerMovement.OnDashStart -= HandleDash;
    }

    void Start()
    {
        // Set global light to tutorial brightness immediately
        if (globalLight != null)
            globalLight.intensity = LIGHT_BRIGHT;

        // Subscribe to dash event
        var pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
        {
            playerMovement = pm;
            playerMovement.OnDashStart += HandleDash;
        }

        StartCoroutine(TutorialSequence());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            playerShot = true;

        if (tutorialFinished && Input.GetKeyDown(KeyCode.Space))
            SceneManager.LoadScene("MainMenu");
    }

    // ─────────────────────────────────────────────────────────────────────────
    IEnumerator TutorialSequence()
    {
        // ── 1. MOVE ──────────────────────────────────────────────────────────
        Show("WASD to move");
        yield return new WaitUntil(() => PlayerMoved());
        Hide();
        yield return new WaitForSeconds(0.5f);

        // ── 2. SLASH ─────────────────────────────────────────────────────────
        Show("Press J to Slash");
        yield return new WaitUntil(() => enemiesKilled >= 1);

        // ── 3. AVOID THE TRAP (2 s) ──────────────────────────────────────────
        Show("Avoid the trap");
        yield return new WaitForSeconds(2f);

        // ── 4. DASH TIP ──────────────────────────────────────────────────────
        // Show the hint, then wait for player to actually dash,
        // then keep it visible for 3.5 more seconds so they can read it.
        playerDashed = false;
        Show("Use Left Shift to dash and avoid the trap");
        yield return new WaitUntil(() => playerDashed);
        yield return new WaitForSeconds(3.5f);
        Hide();
        yield return new WaitForSeconds(0.4f);

        // ── 5. SHOOT ─────────────────────────────────────────────────────────
        // Wait for player to shoot (K), then wait for an enemy to die,
        // then wait 2.5 more seconds before moving on.
        enemiesKilled = 0;
        playerShot    = false;
        Show("Press K to Shoot");
        yield return new WaitUntil(() => playerShot);
        yield return new WaitUntil(() => enemiesKilled >= 1);
        yield return new WaitForSeconds(2.5f);
        Hide();
        yield return new WaitForSeconds(0.4f);

        // ── 6. DASH DAMAGE TIP ───────────────────────────────────────────────
        Show("Dash can also damage enemies!");
        yield return new WaitForSeconds(2.5f);

        // ── 7. DARKEN THE SCENE ──────────────────────────────────────────────
        Show("Now let's make things darker...");
        yield return StartCoroutine(FadeGlobalLight(LIGHT_BRIGHT, LIGHT_DARK, 3f));
        Hide();
        yield return new WaitForSeconds(1f);   // 1 s of silence in the dark

        // ── 8. FLASH ─────────────────────────────────────────────────────────
        Show("Press L to Flash");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.L));
        Hide();
        yield return new WaitForSeconds(2f);

        // ── 9. FLASH WARNING ─────────────────────────────────────────────────
        Show("Careful! Flashing can bring enemies close to you");
        yield return new WaitForSeconds(3f);

        // ── DONE ─────────────────────────────────────────────────────────────
        Show("Tutorial Complete\nPress SPACE to return to Menu");
        tutorialFinished = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    IEnumerator FadeGlobalLight(float from, float to, float duration)
    {
        if (globalLight == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            globalLight.intensity = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        globalLight.intensity = to;
    }

    // Helpers ─────────────────────────────────────────────────────────────────
    void Show(string msg)
    {
        if (tutorialText == null) return;
        tutorialText.text = msg;
        tutorialText.gameObject.SetActive(true);
    }

    void Hide()
    {
        if (tutorialText == null) return;
        tutorialText.gameObject.SetActive(false);
    }

    void HandleEnemyKilled() => enemiesKilled++;
    void HandleDash()        => playerDashed = true;

    bool PlayerMoved()
    {
        return Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;
    }
}