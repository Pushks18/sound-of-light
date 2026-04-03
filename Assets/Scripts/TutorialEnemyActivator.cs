using UnityEngine;

/// <summary>
/// Place a trigger zone at a room entrance.
/// When the player enters, all listed enemies immediately start hunting.
/// Use this for rooms where enemies "immediately ambush" without needing light.
/// One-shot: destroys itself after firing.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TutorialEnemyActivator : MonoBehaviour
{
    [Tooltip("Enemies to force-activate the moment the player enters this zone")]
    public EnemyAI[] enemies;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        foreach (var e in enemies)
            if (e != null) e.ActivateHunt();
        Destroy(gameObject);
    }
}
