using UnityEngine;

/// <summary>
/// Place an invisible trigger collider at the entrance of each tutorial room.
/// Set roomIndex: 1=Slash/Dark room, 2=Shoot room, 3=Trap room, 4=Final room.
/// One-shot: destroys itself after firing so it only triggers once.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TutorialRoomTrigger : MonoBehaviour
{
    [Tooltip("1=Slash/Dark room  2=Shoot room  3=Trap room  4=Final room")]
    public int roomIndex = 1;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        TutorialManager.Instance?.EnterRoom(roomIndex);
        Destroy(gameObject);
    }
}
