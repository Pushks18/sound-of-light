using UnityEngine;

/// <summary>
/// Attach to the Player in the tutorial scene.
/// Resets health every frame so the player can never die.
/// They still flash red when hit (feedback) but health never reaches 0.
/// TutorialLayoutGenerator adds this automatically on Start.
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
        if (hp != null && hp.currentHealth < hp.maxHealth)
            hp.currentHealth = hp.maxHealth;
    }
}
