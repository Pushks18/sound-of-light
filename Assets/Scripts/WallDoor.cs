using UnityEngine;

public class WallDoor : MonoBehaviour
{
    [SerializeField] private int doorNumber;
    
    public int GetDoorNumber()
    {
        return doorNumber;
    }
}
