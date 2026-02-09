using UnityEngine;

public class Door : MonoBehaviour
{
    public string requiredKey = "RedKey";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerInventory inv = other.GetComponent<PlayerInventory>();

        if (inv != null && inv.HasKey(requiredKey))
        {
            OpenDoor();
        }
    }

    void OpenDoor()
    {
        Debug.Log("Door opened");
        gameObject.SetActive(false); // placeholder open
    }
}