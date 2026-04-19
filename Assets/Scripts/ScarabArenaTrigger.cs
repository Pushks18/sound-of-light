using UnityEngine;
using System.Collections;

/// <summary>
/// Place on the single arena-entrance trigger zone above the player.
///
/// Intro sequence on player entry:
///   1. One-shot guard — disables own collider immediately.
///   2. Disable player input + stop velocity.
///   3. TriggerIntroLightBurst on Scarab — body light spikes when camera arrives.
///   4. BossIntroCam.PlayIntro — handles full camera pan, hold, and pan-back.
///   5. Re-enable player input.
///   6. ScarabAI.StartBattle() — shows health bar and begins Phase 1.
///   7. Disable this GameObject.
/// </summary>
public class ScarabArenaTrigger : MonoBehaviour
{
    [SerializeField] private ScarabAI scarab;

    void Awake()
    {
        if (scarab == null)
            scarab = FindAnyObjectByType<ScarabAI>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (scarab == null || !scarab.TryScheduleIntro()) return;

        // Disable own collider immediately — prevents re-entry
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
            if (mb is PlayerMovement  || mb is PlayerShooting  || mb is PlayerSlash ||
                mb is PlayerDash      || mb is PlayerLightWave || mb is FlashlightAim)
                mb.enabled = false;
        }

        var rb = playerObj.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // ── 2. Full fly-in (handles camera internally) ───────────────────────
        if (scarab != null)
            yield return StartCoroutine(scarab.RunFlyInEntrance());

        // ── 3. Re-enable player input ────────────────────────────────────────
        foreach (var mb in playerScripts)
        {
            if (mb is PlayerMovement  || mb is PlayerShooting  || mb is PlayerSlash ||
                mb is PlayerDash      || mb is PlayerLightWave || mb is FlashlightAim)
                mb.enabled = true;
        }

        // ── 4. Start battle ──────────────────────────────────────────────────
        if (scarab != null)
            scarab.StartBattle();

        gameObject.SetActive(false);
    }
}
