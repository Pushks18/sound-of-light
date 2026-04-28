using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent (DontDestroyOnLoad) controller for the Demo Mode scene chain.
/// Activated only via the main-menu "demo" cheat code.
///
/// The demo plays a typed sequence of steps:
///   • Endless step → loads ProgressiveRoomGen, seeds DungeonManager so the run
///     enters the chosen room index (e.g., room 6, 12, 18). One room cleared
///     advances the demo.
///   • Boss step    → loads a boss scene. When the boss is killed and the
///     player walks into the post-boss portal, the demo advances.
///
/// All challenge-cleared signals come from RoomClearPortal calling
/// NotifyChallengeCleared() — the same hook covers both endless rooms (where
/// DungeonManager spawns the portal) and boss arenas (where GameManager spawns it).
///
/// Health is forced per step. The current step's intended HP is applied via
/// PlayerHealth.SaveForSceneLoad before each scene load, AND re-applied a frame
/// after each scene load via a coroutine so that death-restarts also pick up
/// the correct HP (since PlayerHealth.Awake consumes SaveForSceneLoad after one
/// use).
///
/// After the last sequence entry completes, spawns DemoCreditsOverlay which
/// rolls "Thank You" + team names, then returns to the main menu.
/// </summary>
public class DemoSequenceManager : MonoBehaviour
{
    public static DemoSequenceManager Instance;

    public enum StepType { Endless, Boss }

    [Serializable]
    public class DemoStep
    {
        [Tooltip("Endless = loads ProgressiveRoomGen at the chosen room index; Boss = loads the boss scene and waits for post-victory portal.")]
        public StepType type = StepType.Endless;

        [Tooltip("Scene to load. Endless steps default to ProgressiveRoomGen.")]
        public string sceneName = "ProgressiveRoomGen";

        [Tooltip("(Endless only) DungeonManager room index to seed before loading. e.g. 6 spawns a Room 6-sized layout.")]
        public int endlessRoomIndex = 6;

        [Tooltip("Player max HP (and current HP) at the start of this step. Health progresses across the demo.")]
        public int health = 6;
    }

    [Header("Sequence")]
    [Tooltip("Steps played in order. Default matches the showcase demo: endless rooms 6/12/18 with bosses interleaved, HP +2 per step.")]
    [SerializeField]
    private DemoStep[] sequence = new DemoStep[]
    {
        new DemoStep { type = StepType.Endless, sceneName = "ProgressiveRoomGen", endlessRoomIndex = 6,  health = 6  },
        new DemoStep { type = StepType.Boss,    sceneName = "ScarabScene",                              health = 8  }, // Boss 1
        new DemoStep { type = StepType.Endless, sceneName = "ProgressiveRoomGen", endlessRoomIndex = 12, health = 10 },
        new DemoStep { type = StepType.Boss,    sceneName = "VesperScene",                              health = 12 }, // Boss 2
        new DemoStep { type = StepType.Endless, sceneName = "ProgressiveRoomGen", endlessRoomIndex = 18, health = 14 },
        new DemoStep { type = StepType.Boss,    sceneName = "GoblinBossScene",                          health = 16 }, // Boss 3 (Crimson)
    };

    int currentStep = -1;
    bool isActive = false;
    bool advanceLatch = false;

    public static bool IsActive => Instance != null && Instance.isActive;
    public static int CurrentStep => Instance != null ? Instance.currentStep : -1;

    /// <summary>Entry point. Called by MainMenuController when the cheat is typed.</summary>
    public static void StartDemo()
    {
        if (Instance == null)
        {
            var go = new GameObject("DemoSequenceManager");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<DemoSequenceManager>();
        }
        Instance.BeginSequence();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()  { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void BeginSequence()
    {
        if (sequence == null || sequence.Length == 0)
        {
            Debug.LogWarning("[DemoSequenceManager] Empty sequence — nothing to play.");
            return;
        }

        Debug.Log("[DemoSequenceManager] Starting demo sequence.");
        isActive = true;
        currentStep = 0;
        advanceLatch = false;

        ApplyStepStaticState(sequence[0]);
        SceneManager.LoadScene(sequence[0].sceneName);
    }

    /// <summary>
    /// Called by RoomClearPortal when the player enters the post-clear portal —
    /// fires for both endless-room clears and boss-defeat clears. Advances the demo.
    /// </summary>
    public void NotifyChallengeCleared()
    {
        Advance();
    }

    /// <summary>Move to the next step. Public so any custom victory hook can call it.</summary>
    public void Advance()
    {
        if (!isActive || advanceLatch) return;
        advanceLatch = true;

        currentStep++;
        if (currentStep >= sequence.Length)
        {
            ShowCreditsAndEnd();
            return;
        }

        var step = sequence[currentStep];
        Debug.Log($"[DemoSequenceManager] Step {currentStep}: {step.type} → {step.sceneName} (HP {step.health}, room {step.endlessRoomIndex}).");
        ApplyStepStaticState(step);
        SceneManager.LoadScene(step.sceneName);
    }

    /// <summary>
    /// Set the static fields the new scene's Start() methods will read:
    ///   • PlayerHealth.SaveForSceneLoad → seeds initial HP on first load
    ///   • DungeonManager.DemoStartRoomIndex → seeds the room index for endless steps
    /// </summary>
    void ApplyStepStaticState(DemoStep step)
    {
        PlayerHealth.SaveForSceneLoad(step.health, step.health);

        if (step.type == StepType.Endless)
            DungeonManager.DemoStartRoomIndex = Mathf.Max(1, step.endlessRoomIndex);
        else
            DungeonManager.DemoStartRoomIndex = -1;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isActive) return;
        advanceLatch = false;

        // PlayerHealth.Awake consumes SaveForSceneLoad after one use, so a
        // death-restart of the same scene would default back to 3 HP. Re-apply
        // the step's intended HP one frame after the scene loads (so PlayerHealth
        // has finished Awake/Start) for both initial loads AND restarts.
        if (currentStep >= 0 && currentStep < sequence.Length)
            StartCoroutine(ReapplyHealthNextFrame(sequence[currentStep].health));
    }

    IEnumerator ReapplyHealthNextFrame(int hp)
    {
        yield return null;   // wait for scene Awake/Start
        var ph = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);
        if (ph == null) yield break;

        ph.maxHealth = hp;
        ph.currentHealth = hp;
        StatusHUD.Instance?.UpdateHP(ph.currentHealth, ph.maxHealth);
    }

    void ShowCreditsAndEnd()
    {
        Debug.Log("[DemoSequenceManager] Sequence complete — rolling credits.");

        var creditsGO = new GameObject("DemoCreditsOverlay");
        DontDestroyOnLoad(creditsGO);
        var credits = creditsGO.AddComponent<DemoCreditsOverlay>();
        credits.OnFinished = () =>
        {
            EndDemo();
            SceneManager.LoadScene("MainMenu");
        };
    }

    void EndDemo()
    {
        isActive = false;
        currentStep = -1;
        // Clear demo statics so a future run starts clean
        DungeonManager.DemoStartRoomIndex = -1;
        Destroy(gameObject);
    }
}
