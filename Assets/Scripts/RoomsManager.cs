using UnityEngine;

public class RoomsManager : MonoBehaviour
{
    [SerializeField] private Checkpoint[] checkpoints;
    [SerializeField] private GameObject[] doors;

    private void Awake()
    {
        BuildCheckpointList();
        BuildDoorList();
    }

    private void BuildCheckpointList()
    {
        Checkpoint[] foundCheckpoints = FindObjectsOfType<Checkpoint>();

        int maxCheckpointNumber = 0;
        foreach (Checkpoint cp in foundCheckpoints)
        {
            if (cp == null) continue;

            int num = cp.GetCheckpointNumber();
            if (num > maxCheckpointNumber)
                maxCheckpointNumber = num;
        }

        // Index 0 unused so numbering can start at 1
        checkpoints = new Checkpoint[maxCheckpointNumber + 1];

        foreach (Checkpoint cp in foundCheckpoints)
        {
            if (cp == null) continue;

            int num = cp.GetCheckpointNumber();
            if (num >= 1 && num < checkpoints.Length)
            {
                checkpoints[num] = cp;
            }
        }
    }

    private void BuildDoorList()
    {
        WallDoor[] foundDoors = FindObjectsOfType<WallDoor>();

        int maxDoorNumber = 0;
        foreach (WallDoor door in foundDoors)
        {
            if (door == null) continue;

            int num = door.GetDoorNumber();
            if (num > maxDoorNumber)
                maxDoorNumber = num;
        }

        // Index 0 unused so numbering can start at 1
        doors = new GameObject[maxDoorNumber + 1];

        foreach (WallDoor door in foundDoors)
        {
            if (door == null) continue;

            int num = door.GetDoorNumber();
            if (num >= 1 && num < doors.Length)
            {
                doors[num] = door.gameObject;
            }
        }
    }

    public Vector3?[] GetCheckpointPositions()
    {
        if (checkpoints == null || checkpoints.Length == 0)
            BuildCheckpointList();

        Vector3?[] positions = new Vector3?[checkpoints.Length];

        for (int i = 1; i < checkpoints.Length; i++)
        {
            if (checkpoints[i] != null && checkpoints[i].GetCheckpointActivated())
                positions[i] = checkpoints[i].transform.position;
            else
                positions[i] = null;
        }

        return positions;
    }

    public void DisableDoor(int doorNumber)
    {
        if (doors == null || doors.Length == 0)
            BuildDoorList();

        if (doorNumber < 1 || doorNumber >= doors.Length)
        {
            Debug.LogWarning($"DisableDoor: door number {doorNumber} is out of range.");
            return;
        }

        if (doors[doorNumber] != null)
        {
            doors[doorNumber].SetActive(false);
        }
        else
        {
            Debug.LogWarning($"DisableDoor: no door found for number {doorNumber}.");
        }
    }
}