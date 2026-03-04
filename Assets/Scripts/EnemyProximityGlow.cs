using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EnemyProximityGlow : MonoBehaviour
{
    public float maxDistance = 8f;
    public float maxIntensity = 2f;

    private Light2D enemyLight;
    private Transform player;

    void Start()
    {
        enemyLight = GetComponent<Light2D>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (enemyLight == null) return;

        // Re-acquire player if reference was lost (e.g. respawn)
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null) return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance < maxDistance)
        {
            float t = Mathf.SmoothStep(0f, 1f, 1f - (distance / maxDistance));
            enemyLight.intensity = t * maxIntensity;
        }
        else
        {
            enemyLight.intensity = 0f;
        }
    }
}