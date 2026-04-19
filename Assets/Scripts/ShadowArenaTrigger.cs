using UnityEngine;
using System.Collections;

/// <summary>
/// Place one BoxCollider2D (set to trigger) at each entrance to the Umbra boss
/// arena in NewBoss2Scene. When the player walks in, this trigger:
///   1. Locks player input
///   2. Plays the boss intro camera pan
///   3. Re-enables player input
///   4. Starts the ShadowBossAI fight
/// </summary>
public class ShadowArenaTrigger : MonoBehaviour
{
    [SerializeField] private ShadowBossAI  shadowBoss;
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

        // Mark boss as intro-started immediately so AutoStart doesn't fire during the camera pan
        shadowBoss?.BeginIntro();

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
        if (bossIntroCam != null && shadowBoss != null)
            yield return StartCoroutine(bossIntroCam.PlayIntro(
                shadowBoss.transform.position, playerObj.transform));

        // 3. Re-enable player input
        foreach (var mb in scripts)
        {
            if (mb is PlayerMovement  || mb is PlayerShooting ||
                mb is PlayerSlash     || mb is PlayerDash     ||
                mb is PlayerLightWave || mb is FlashlightAim)
                mb.enabled = true;
        }

        // 4. Start boss
        if (shadowBoss != null) shadowBoss.StartIntroSequence();

        gameObject.SetActive(false);
    }
}
