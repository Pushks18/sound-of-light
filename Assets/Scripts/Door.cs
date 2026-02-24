using UnityEngine;

public class Door : MonoBehaviour
{
    public string requiredKey = "RedKey";

    private bool opened = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Door] OnTriggerEnter2D hit by: {other.name} tag={other.tag}");

        if (opened) { Debug.Log("[Door] Already opened, ignoring"); return; }
        if (!other.CompareTag("Player")) { Debug.Log("[Door] Not player, ignoring"); return; }

        PlayerInventory inv = other.GetComponent<PlayerInventory>();
        Debug.Log($"[Door] PlayerInventory found: {inv != null}, HasKey({requiredKey}): {inv?.HasKey(requiredKey)}");
        Debug.Log($"[Door] GameManager.Instance: {GameManager.Instance != null}, gameEnded: {GameManager.Instance?.gameEnded}");

        if (inv != null && inv.HasKey(requiredKey))
        {
            opened = true;
            GameManager.Instance?.PlayerWon();
            Debug.Log("[Door] PlayerWon() called!");
        }
        else
        {
            DoorMessageUI.Show("You need a key to open this door");
            Debug.Log("[Door] Player missing key");
        }
    }
}
