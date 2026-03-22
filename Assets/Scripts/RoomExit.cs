using UnityEngine;

public class RoomExit : MonoBehaviour
{
    public string direction; // "left", "right", "top", "bottom"

    [Tooltip("If true, player must have requiredKeyId before LoadNextRoom runs.")]
    public bool requireKey;
    public string requiredKeyId = "RedKey";
    [Tooltip("If true, one matching key is removed when leaving through this exit.")]
    public bool consumeKeyOnExit = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (DungeonManager.Instance == null) return;

        if (requireKey)
        {
            PlayerInventory inv = other.GetComponent<PlayerInventory>();
            if (inv == null || !inv.HasKey(requiredKeyId))
            {
                DoorMessageUI.Show("You need a key to leave this room");
                return;
            }
            if (consumeKeyOnExit)
                inv.ConsumeKey(requiredKeyId);
        }

        DungeonManager.Instance.LoadNextRoom(direction);
    }
}