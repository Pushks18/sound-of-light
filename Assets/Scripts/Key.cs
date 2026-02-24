using UnityEngine;

public class Key : MonoBehaviour
{
    public string keyID = "RedKey";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInventory inv = other.GetComponent<PlayerInventory>();
            if (inv != null)
            {
                inv.AddKey(keyID);

                // If a KeyItem visual is attached, make it follow the player
                // instead of destroying the whole object.
                var keyItem = GetComponent<KeyItem>();
                if (keyItem != null)
                {
                    keyItem.PickUp(other.transform);
                    // Disable this script so the trigger doesn't fire again
                    enabled = false;
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}