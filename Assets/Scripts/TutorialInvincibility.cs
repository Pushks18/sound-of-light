using UnityEngine;

/// <summary>
/// Attach to the Player in the tutorial scene.
/// Health can drop from damage but never below 1 — the player feels
/// danger but cannot die. TutorialLayoutGenerator adds this automatically.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class TutorialInvincibility : MonoBehaviour
{
    private PlayerHealth hp;

    void Start()
    {
        hp = GetComponent<PlayerHealth>();
    }

    void LateUpdate()
    {
        if (hp != null && hp.currentHealth < 1)
            hp.currentHealth = 1;
    }
}
