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
                Destroy(gameObject);
            }
        }
    }
}