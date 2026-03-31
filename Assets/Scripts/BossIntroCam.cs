using UnityEngine;
using System.Collections;

/// <summary>
/// Handles all camera pan sequences for the Vesper boss scene.
///
/// Three modes:
///   PlayIntro      — called by VesperArenaTrigger: pan to boss → hold → pan back to player
///   GlanceAtBoss   — triggered automatically on EnemyHealth.OnEnemyKilled: brief pan to boss → pan back
///   FocusOnBoss /
///   PanBackToPlayer — called by VesperAI.DeathSequence: snap to boss for death anim, then pan back
///
/// Assign cam, cameraFollow, vesper, and playerTransform in Inspector.
/// </summary>
public class BossIntroCam : MonoBehaviour
{
    [Tooltip("The Main Camera.")]
    [SerializeField] private Camera cam;

    [Tooltip("The CameraFollow component on the Main Camera.")]
    [SerializeField] private CameraFollow cameraFollow;

    [Header("Intro Pan")]
    [SerializeField] private float panToTargetDuration = 2f;
    [SerializeField] private float holdDuration        = 2.5f;
    [SerializeField] private float panBackDuration     = 1.5f;

    [Header("Enemy Death Glance")]
    [SerializeField] private VesperAI  vesper;
    [SerializeField] private Transform playerTransform;  // leave empty — auto-found at Start
    [SerializeField] private float glancePanToDuration   = 0.6f;
    [SerializeField] private float glanceHoldDuration    = 0.4f;
    [SerializeField] private float glancePanBackDuration = 0.8f;

    [Header("Boss Death Pan Back")]
    [SerializeField] private float deathPanBackDuration = 2f;

    bool isBusy = false;

    void Start()
    {
        if (playerTransform == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }
    }

    void OnEnable()  => EnemyHealth.OnEnemyKilled += HandleEnemyKilled;
    void OnDisable() => EnemyHealth.OnEnemyKilled -= HandleEnemyKilled;

    // ── Enemy Death Glance ───────────────────────────────────────────────────

    void HandleEnemyKilled()
    {
        if (isBusy) return;
        if (vesper == null || playerTransform == null) return;
        if (!vesper.IsInBattle) return;
        StartCoroutine(GlanceAtBoss());
    }

    IEnumerator GlanceAtBoss()
    {
        isBusy = true;
        if (cam == null) cam = Camera.main;

        if (cameraFollow != null) cameraFollow.enabled = false;

        Vector3 bossPos = new Vector3(vesper.transform.position.x, vesper.transform.position.y, -10f);
        yield return StartCoroutine(PanTo(bossPos, glancePanToDuration));

        yield return new WaitForSeconds(glanceHoldDuration);

        if (playerTransform != null)
        {
            Vector3 playerPos = new Vector3(playerTransform.position.x, playerTransform.position.y, -10f);
            yield return StartCoroutine(PanTo(playerPos, glancePanBackDuration));
            if (cam != null)
                cam.transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y, -10f);
        }

        if (cameraFollow != null) cameraFollow.enabled = true;
        isBusy = false;
    }

    // ── Boss Intro ───────────────────────────────────────────────────────────

    /// <summary>
    /// Called by VesperArenaTrigger. Pan to boss, hold, then smoothly pan back to player.
    /// </summary>
    public IEnumerator PlayIntro(Vector3 targetWorldPos, Transform player)
    {
        isBusy = true;
        if (cam == null) cam = Camera.main;

        if (cameraFollow != null) cameraFollow.enabled = false;

        // Pan to boss
        Vector3 bossPos = new Vector3(targetWorldPos.x, targetWorldPos.y, -10f);
        yield return StartCoroutine(PanTo(bossPos, panToTargetDuration));

        // Hold on boss
        yield return new WaitForSeconds(holdDuration);

        // Smoothly pan back to player (not a snap)
        if (player != null)
        {
            Vector3 playerPos = new Vector3(player.position.x, player.position.y, -10f);
            yield return StartCoroutine(PanTo(playerPos, panBackDuration));
            if (cam != null)
                cam.transform.position = new Vector3(player.position.x, player.position.y, -10f);
        }

        if (cameraFollow != null) cameraFollow.enabled = true;
        isBusy = false;
    }

    // ── Boss Death ───────────────────────────────────────────────────────────

    /// <summary>
    /// Immediately snap camera to boss position and disable CameraFollow.
    /// Call at the start of VesperAI.DeathSequence so the death animation
    /// is shown at the boss's location.
    /// </summary>
    public void FocusOnBoss(Vector3 bossWorldPos)
    {
        if (cam == null) cam = Camera.main;
        if (cameraFollow != null) cameraFollow.enabled = false;
        if (cam != null)
            cam.transform.position = new Vector3(bossWorldPos.x, bossWorldPos.y, -10f);
        isBusy = true;
    }

    /// <summary>
    /// Smoothly pan camera from current position (boss) back to player,
    /// then re-enable CameraFollow. Call after the boss death animation finishes.
    /// </summary>
    public IEnumerator PanBackToPlayer(Transform player)
    {
        if (cam == null) cam = Camera.main;

        if (player != null)
        {
            Vector3 playerPos = new Vector3(player.position.x, player.position.y, -10f);
            yield return StartCoroutine(PanTo(playerPos, deathPanBackDuration));
            if (cam != null)
                cam.transform.position = new Vector3(player.position.x, player.position.y, -10f);
        }

        if (cameraFollow != null) cameraFollow.enabled = true;
        isBusy = false;
    }

    // ── Phase 2 Transition ───────────────────────────────────────────────────

    /// <summary>
    /// Smoothly pan to the boss for the Phase 2 cinematic. Disables CameraFollow.
    /// VesperAI yields on this so the camera arrives before the teleport loop starts.
    /// </summary>
    public IEnumerator PanToBossPhase2(Vector3 bossWorldPos, float duration)
    {
        if (cam == null) cam = Camera.main;
        if (cameraFollow != null) cameraFollow.enabled = false;
        isBusy = true;
        Vector3 target = new Vector3(bossWorldPos.x, bossWorldPos.y, -10f);
        yield return StartCoroutine(PanTo(target, duration));
    }

    /// <summary>
    /// Instantly move the camera to a world position.
    /// Used during Phase 2 teleport loop so the camera tracks each teleport.
    /// </summary>
    public void SnapToPosition(Vector3 worldPos)
    {
        if (cam == null) cam = Camera.main;
        if (cam != null)
            cam.transform.position = new Vector3(worldPos.x, worldPos.y, -10f);
    }

    // ── Shared ───────────────────────────────────────────────────────────────

    IEnumerator PanTo(Vector3 target, float duration)
    {
        if (cam == null) yield break;
        Vector3 from    = cam.transform.position;
        float   elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            cam.transform.position = Vector3.Lerp(from, target, t);
            yield return null;
        }
        cam.transform.position = target;
    }
}
