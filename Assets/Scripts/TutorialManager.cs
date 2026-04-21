using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI")]
    public TMP_Text tutorialText;

    [Header("Global Light")]
    [Tooltip("Drag the Global Light 2D GameObject here.")]
    public Light2D globalLight;

    // ── Light levels ─────────────────────────────────────────────────────────
    private const float LIGHT_BRIGHT = 0.6f;     // visible — player can see the room
    private const float LIGHT_DIM    = 0f;       // pitch black — teaches flash mechanic

    // ── State ─────────────────────────────────────────────────────────────────
    private int  currentRoom      = 0;   // which tutorial room the player is in
    private int  enemiesKilled    = 0;
    private bool tutorialFinished = false;
    private bool playerDashed     = false;
    private bool playerShot       = false;
    private bool playerFlashed    = false;

    private PlayerMovement playerMovement;
    private GameObject bgPanel;   // dark backdrop behind tutorial text

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()  => EnemyHealth.OnEnemyKilled += HandleEnemyKilled;
    void OnDisable()
    {
        EnemyHealth.OnEnemyKilled -= HandleEnemyKilled;
        if (playerMovement != null)
            playerMovement.OnDashStart -= HandleDash;
    }

    void Start()
    {
        if (globalLight != null)
            globalLight.intensity = LIGHT_BRIGHT;

        var pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
        {
            playerMovement = pm;
            playerMovement.OnDashStart += HandleDash;
        }

        SetupTextStyle();
        StartCoroutine(Room0_Intro());
    }

    void SetupTextStyle()
    {
        if (tutorialText == null) return;

        // ── Make text larger and bolder ──
        tutorialText.fontSize = 48;
        tutorialText.fontStyle = FontStyles.Bold;
        tutorialText.textWrappingMode = TextWrappingModes.Normal;
        tutorialText.alignment = TextAlignmentOptions.Center;

        // ── Widen the rect so longer lines don't clip ──
        var rt = tutorialText.GetComponent<RectTransform>();
        if (rt != null)
            rt.sizeDelta = new Vector2(800, 200);

        // ── Dark semi-transparent backdrop behind the text ──
        bgPanel = new GameObject("TutorialBG");
        bgPanel.transform.SetParent(tutorialText.transform.parent, false);
        // Put it behind the text in sibling order
        bgPanel.transform.SetSiblingIndex(tutorialText.transform.GetSiblingIndex());

        var bgRT = bgPanel.AddComponent<RectTransform>();
        // Copy anchoring from the text
        bgRT.anchorMin = rt.anchorMin;
        bgRT.anchorMax = rt.anchorMax;
        bgRT.pivot     = rt.pivot;
        bgRT.anchoredPosition = rt.anchoredPosition;
        bgRT.sizeDelta = rt.sizeDelta + new Vector2(60, 40);  // padding around text

        var img = bgPanel.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.7f);
        img.raycastTarget = false;

        bgPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) playerShot    = true;
        if (Input.GetKeyDown(KeyCode.L)) playerFlashed = true;

        if (tutorialFinished &&
            (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)))
            SceneManager.LoadScene("MainMenu");
    }

    // ── Called by TutorialRoomTrigger ─────────────────────────────────────────
    public void EnterRoom(int roomIndex)
    {
        if (roomIndex <= currentRoom) return;   // ignore duplicate triggers
        currentRoom = roomIndex;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ROOM 0 – Starting room
    //  Safe room with green light sources. Intro text shown as player explores.
    // ═════════════════════════════════════════════════════════════════════════
    IEnumerator Room0_Intro()
    {
        yield return new WaitForSeconds(0.8f);
        Show("It's Prolly scary in there\nBut everything in you\nis made of Light");

        // Wait until the player walks into room 1
        yield return new WaitUntil(() => currentRoom >= 1);
        Hide();

        yield return StartCoroutine(Room1_SlashAndFlash());
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ROOM 1 – Dark room  (teaches: L to flash, J to slash)
    //  Enemies here activate naturally when the player's flash hits them.
    //  Place 2 enemies: one near the entrance, one behind the player's path.
    // ═════════════════════════════════════════════════════════════════════════
    IEnumerator Room1_SlashAndFlash()
    {
        enemiesKilled = 0;

        // Dim the scene to signal darkness
        yield return StartCoroutine(FadeGlobalLight(LIGHT_BRIGHT, LIGHT_DIM, 0.6f));

        // Step 1: teach the flash
        playerFlashed = false;
        Show("Too Dark?\nHit L to light up your surroundings");
        yield return new WaitUntil(() => playerFlashed);
        Hide();
        yield return new WaitForSeconds(0.4f);

        // Step 2: teach the slash (enemies are now visible + activated by the flash)
        Show("SPAM J to kill");
        yield return new WaitUntil(() => enemiesKilled >= 2);
        Hide();
        yield return new WaitForSeconds(0.5f);

        // Stay dark from here on — the real game is dark
        yield return new WaitUntil(() => currentRoom >= 2);
        yield return StartCoroutine(Room2_Shoot());
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ROOM 2 – Ambush room  (teaches: K to shoot)
    //  Place 3 enemies that are immediately activated via TutorialEnemyActivator.
    // ═════════════════════════════════════════════════════════════════════════
    IEnumerator Room2_Shoot()
    {
        enemiesKilled = 0;
        playerShot    = false;

        Show("Enemy far away?\nTake aim as you move and shoot them with K!");
        yield return new WaitUntil(() => playerShot);
        yield return new WaitUntil(() => enemiesKilled >= 1);
        Hide();
        yield return new WaitForSeconds(0.5f);

        yield return new WaitUntil(() => currentRoom >= 3);
        yield return StartCoroutine(Room3_Dash());
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ROOM 3 – Trap corridor  (teaches: Left-Shift to dash)
    //  Fill the corridor with traps (startArmed = true on each Trap component).
    // ═════════════════════════════════════════════════════════════════════════
    IEnumerator Room3_Dash()
    {
        playerDashed = false;

        Show("Hmm, Maybe try dashing\nacross with L-Shift");
        yield return new WaitUntil(() => playerDashed);
        Hide();
        yield return new WaitForSeconds(0.5f);

        yield return new WaitUntil(() => currentRoom >= 4);
        yield return StartCoroutine(Room4_Final());
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ROOM 4 – Final room
    //  One enemy (or none) + plenty of green light sources as a cool backdrop.
    // ═════════════════════════════════════════════════════════════════════════
    IEnumerator Room4_Final()
    {
        Show("Do you think you are ready?\nDon't worry, Nobody is...");
        yield return new WaitForSeconds(3.5f);

        Show("Tutorial Complete\nClick Escape or Space to go to the Menu");
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

    void Show(string msg)
    {
        if (tutorialText == null) return;
        tutorialText.text = msg;
        tutorialText.gameObject.SetActive(true);
        if (bgPanel != null) bgPanel.SetActive(true);
    }

    void Hide()
    {
        if (tutorialText == null) return;
        tutorialText.gameObject.SetActive(false);
        if (bgPanel != null) bgPanel.SetActive(false);
    }

    void HandleEnemyKilled() => enemiesKilled++;
    void HandleDash()        => playerDashed = true;
}
