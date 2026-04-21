using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private int checkpointNumber;
    [SerializeField] private bool checkpointActivated = false;
    [SerializeField] private Light2D ambientLight;
        private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            checkpointActivated = true;
            if (ambientLight != null)
            {
                ambientLight.intensity = 3f;
            }
        }
    }
    public bool GetCheckpointActivated()
    {
        return checkpointActivated;
    }
    public int GetCheckpointNumber()
    {
        return checkpointNumber;
    }
}
