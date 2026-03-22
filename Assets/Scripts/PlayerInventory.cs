using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    private HashSet<string> keys = new HashSet<string>();

    public void AddKey(string keyID)
    {
        keys.Add(keyID);
        Debug.Log("Picked up key: " + keyID);
    }

    public bool HasKey(string keyID)
    {
        return keys.Contains(keyID);
    }

    public bool ConsumeKey(string keyID)
    {
        return keys.Remove(keyID);
    }
}