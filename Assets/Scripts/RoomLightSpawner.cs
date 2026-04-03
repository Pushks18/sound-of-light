using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Randomly scatters stationary light sources inside a room at runtime.
///
/// Setup:
///   1. Add this component to any empty GameObject inside a tutorial room.
///   2. Position it at the centre of the room.
///   3. Set roomSize to match the inner walkable area of the room.
///   4. Hit Play — it spawns `count` lights randomly inside that box.
///
/// The spawned lights are tagged "LightSource" with a CircleCollider2D trigger,
/// so EnemyAI activates when they walk into them — same as the main game.
/// </summary>
public class RoomLightSpawner : MonoBehaviour
{
    [Header("Room bounds")]
    [Tooltip("Inner walkable size of the room (width x height) in world units.")]
    public Vector2 roomSize = new Vector2(16f, 12f);

    [Header("Light settings")]
    public int   count     = 6;
    public float radius    = 4f;       // outer light radius
    public float intensity = 1.2f;
    public Color color     = new Color(0.55f, 1f, 0.55f);   // green, like the diagram

    [Header("Placement")]
    [Tooltip("Minimum distance between two spawned lights.")]
    public float minDistBetween = 3f;
    [Tooltip("Keep lights this far from the room edge so they don't clip into walls.")]
    public float edgeMargin     = 1.5f;

    void Start()
    {
        SpawnLights();
    }

    void SpawnLights()
    {
        float halfW = roomSize.x * 0.5f - edgeMargin;
        float halfH = roomSize.y * 0.5f - edgeMargin;
        Vector2 centre = transform.position;

        var placed = new System.Collections.Generic.List<Vector2>();
        int  attempts = count * 20;   // give up if we can't find a spot

        for (int i = 0; i < count && attempts > 0; attempts--)
        {
            float x = Random.Range(-halfW, halfW);
            float y = Random.Range(-halfH, halfH);
            Vector2 pos = centre + new Vector2(x, y);

            // Check minimum spacing
            bool tooClose = false;
            foreach (var p in placed)
            {
                if (Vector2.Distance(pos, p) < minDistBetween)
                { tooClose = true; break; }
            }
            if (tooClose) continue;

            SpawnOneLight(pos);
            placed.Add(pos);
            i++;
        }
    }

    void SpawnOneLight(Vector2 worldPos)
    {
        var go = new GameObject("RoomLight");
        go.transform.position = worldPos;
        go.tag = "LightSource";

        // Point Light 2D
        var light2d = go.AddComponent<Light2D>();
        light2d.lightType             = Light2D.LightType.Point;
        light2d.color                 = color;
        light2d.intensity             = intensity;
        light2d.pointLightOuterRadius = radius;
        light2d.pointLightInnerRadius = radius * 0.3f;
        light2d.pointLightOuterAngle  = 360f;
        light2d.pointLightInnerAngle  = 360f;
        light2d.shadowsEnabled        = true;
        light2d.falloffIntensity      = 0.6f;

        // Trigger collider so EnemyAI activates inside the light
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = radius;
    }

    // Draw the room bounds in the Scene view for easy sizing
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.25f);
        Gizmos.DrawCube(transform.position, new Vector3(roomSize.x, roomSize.y, 0.1f));
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.8f);
        Gizmos.DrawWireCube(transform.position, new Vector3(roomSize.x, roomSize.y, 0.1f));
    }
}
