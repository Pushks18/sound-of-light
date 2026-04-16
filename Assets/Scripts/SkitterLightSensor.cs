using UnityEngine;

// Child sensor on SkitterEnemy — routes LightSource trigger callbacks to SkitterAI.
public class SkitterLightSensor : MonoBehaviour
{
    [HideInInspector] public SkitterAI owner;

    void OnTriggerStay2D(Collider2D other)
    {
        if (owner != null && other.CompareTag("LightSource"))
            owner.NotifyLightNearby();
    }
}
