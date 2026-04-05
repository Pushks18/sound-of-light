using UnityEngine;
using System.Collections;

/// <summary>
/// Place one instance on each of the three arena-entrance trigger zones
/// (left, front, right). All three must reference the same VesperAI,
/// the same BossIntroCam, and each other.
///
/// Sequence on player entry:
///   1. Static guard — only the first trigger to fire proceeds
///   2. Disable other triggers; disable own Collider (keeps coroutine alive)
///   3. Disable player input + stop velocity
///   4. BossIntroCam.PlayIntro — handles camera pan entirely
///   5. Re-enable player input
///   6. StartIntroSequence() on Vesper
///   7. Disable this GameObject
/// </summary>
public class VesperArenaTrigger : MonoBehaviour
{
    [SerializeField] private VesperAI     vesper;
    [SerializeField] private BossIntroCam bossIntroCam;

    [Tooltip("All three entrance trigger GameObjects (including this one).")]
    [SerializeField] private GameObject[] allTriggers;

    // Static flag — shared across all instances, guaranteed one-shot even if
    // multiple triggers fire in the same physics frame.
    private static bool introStarted = false;

    void Awake()
    {
        // Auto-find serialized refs when spawned via DungeonManager prefab
        // (Inspector assignments take priority; these only fill in nulls)
        if (vesper == null)
            vesper = GetComponentInParent<VesperAI>();
        if (bossIntroCam == null)
            bossIntroCam = FindAnyObjectByType<BossIntroCam>();
    }

    void OnEnable()  { introStarted = false; }
    void OnDestroy() { introStarted = false; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (introStarted) return;
        if (!other.CompareTag("Player")) return;

        introStarted = true;

        foreach (var t in allTriggers)
            if (t != null && t != gameObject) t.SetActive(false);

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        StartCoroutine(IntroSequence(other.gameObject));
    }

    IEnumerator IntroSequence(GameObject playerObj)
    {
        // ── 1. Disable player input ──────────────────────────────────────────
        var playerScripts = playerObj.GetComponents<MonoBehaviour>();
        foreach (var mb in playerScripts)
        {
            if (mb is PlayerMovement  ||
                mb is PlayerShooting  ||
                mb is PlayerSlash     ||
                mb is PlayerDash      ||
                mb is PlayerLightWave ||
                mb is FlashlightAim)
            {
                mb.enabled = false;
            }
        }

        var rb = playerObj.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // ── 2. Camera intro ──────────────────────────────────────────────────
        // Trigger eye fade-in timed to when the camera arrives at the boss
        if (vesper != null && bossIntroCam != null)
            vesper.TriggerIntroEyeFade(bossIntroCam.PanToTargetDuration, 1.5f);

        if (bossIntroCam != null)
            yield return StartCoroutine(bossIntroCam.PlayIntro(
                vesper != null ? vesper.transform.position : Vector3.zero,
                playerObj.transform));

        // ── 3. Re-enable player input ────────────────────────────────────────
        foreach (var mb in playerScripts)
        {
            if (mb is PlayerMovement  ||
                mb is PlayerShooting  ||
                mb is PlayerSlash     ||
                mb is PlayerDash      ||
                mb is PlayerLightWave ||
                mb is FlashlightAim)
            {
                mb.enabled = true;
            }
        }

        // ── 4. Vesper enters idle-waiting state ──────────────────────────────
        if (vesper != null)
            vesper.StartIntroSequence();

        // ── 5. Self-cleanup ──────────────────────────────────────────────────
        gameObject.SetActive(false);
    }
}
