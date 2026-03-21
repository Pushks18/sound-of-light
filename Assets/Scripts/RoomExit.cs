using UnityEngine;

public class RoomExit : MonoBehaviour
{
    public string direction; // "left", "right", "top", "bottom"

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            DungeonManager.Instance.LoadNextRoom(direction);
        }
    }
}