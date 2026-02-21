using UnityEngine;

public class FinishLine : MonoBehaviour
{
    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;
            Debug.Log("Player reached finish line");

            GameManager.Instance.PlayerWon();
        }
    }
}