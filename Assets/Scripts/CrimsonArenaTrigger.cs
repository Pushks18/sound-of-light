using UnityEngine;
using System.Collections;

/// <summary>
/// Place one BoxCollider2D trigger at each entrance to the Crimson boss arena.
/// When the player walks in, the trigger:
///   1. Locks player input
///   2. Plays the boss intro camera pan
///   3. Re-enables player input
///   4. Starts the Crimson boss fight
/// </summary>
public class CrimsonArenaTrigger : MonoBehaviour
{
    [SerializeField] private CrimsonAI     crimson;
    [SerializeField] private BossIntroCam  bossIntroCam;

    [Tooltip("All entrance trigger GameObjects (including this one) to deactivate after the first fires.")]
    [SerializeField] private GameObject[] allTriggers;

    private static bool introStarted = false;

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
        // 1. Disable player input
        var scripts = playerObj.GetComponents<MonoBehaviour>();
        foreach (var mb in scripts)
        {
            if (mb is PlayerMovement  || mb is PlayerShooting ||
                mb is PlayerSlash     || mb is PlayerDash     ||
                mb is PlayerLightWave || mb is FlashlightAim)
                mb.enabled = false;
        }

        var pRb = playerObj.GetComponent<Rigidbody2D>();
        if (pRb != null) pRb.linearVelocity = Vector2.zero;

        // 2. Boss intro camera pan
        if (bossIntroCam != null && crimson != null)
            yield return StartCoroutine(bossIntroCam.PlayIntro(
                crimson.transform.position, playerObj.transform));

        // 3. Re-enable player input
        foreach (var mb in scripts)
        {
            if (mb is PlayerMovement  || mb is PlayerShooting ||
                mb is PlayerSlash     || mb is PlayerDash     ||
                mb is PlayerLightWave || mb is FlashlightAim)
                mb.enabled = true;
        }

        // 4. Start boss
        if (crimson != null) crimson.StartIntroSequence();

        gameObject.SetActive(false);
    }
}
