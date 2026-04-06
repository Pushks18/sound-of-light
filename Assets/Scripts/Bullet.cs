using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public GameObject impactEchoPrefab;

    [Header("Player Bullet Light")]
    public float bulletLightRadius = 1.2f;
    public float bulletLightIntensity = 0.8f;
    public Color bulletLightColor = new Color(1f, 0.85f, 0.5f);

    private Rigidbody2D rb;
    private static Material cachedSpriteMat;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = transform.up * speed;

        // Enemy bullets emit a faint red light
        if (CompareTag("EnemyBullet"))
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.material = GetSpriteMaterial();
                sr.color = new Color(1f, 0.3f, 0.2f);
                sr.sortingOrder = 10;
            }

            var light = gameObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = new Color(1f, 0.3f, 0.2f);
            light.intensity = 0.5f;
            light.pointLightOuterRadius = 0.8f;
            light.pointLightInnerRadius = 0.15f;
            light.pointLightOuterAngle = 360f;
            light.pointLightInnerAngle = 360f;
            light.shadowsEnabled = false;
        }

        // Player bullets emit light and activate enemies
        if (CompareTag("Bullet"))
        {
            // Make the bullet sprite always visible (unlit) and larger
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.material = GetSpriteMaterial();
                sr.color = bulletLightColor;
                sr.sortingOrder = 10;
                transform.localScale = new Vector3(0.25f, 0.25f, 1f);
            }

            // Attached Light2D so the bullet illuminates surroundings
            var light = gameObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = bulletLightColor;
            light.intensity = bulletLightIntensity;
            light.pointLightOuterRadius = bulletLightRadius;
            light.pointLightInnerRadius = bulletLightRadius * 0.2f;
            light.pointLightOuterAngle = 360f;
            light.pointLightInnerAngle = 360f;
            light.shadowsEnabled = false;

            // Light source trigger so enemies detect the travelling light.
            // Needs its own Rigidbody2D so its collider doesn't route
            // OnTriggerEnter2D callbacks to the parent bullet.
            var lightTrigger = new GameObject("BulletLightTrigger");
            lightTrigger.transform.SetParent(transform, false);
            lightTrigger.transform.localPosition = Vector3.zero;
            lightTrigger.tag = "LightSource";
            var triggerRb = lightTrigger.AddComponent<Rigidbody2D>();
            triggerRb.bodyType = RigidbodyType2D.Kinematic;
            var triggerCol = lightTrigger.AddComponent<CircleCollider2D>();
            triggerCol.isTrigger = true;
            triggerCol.radius = bulletLightRadius;
        }

        Destroy(gameObject, 3f);
    }

    static Material GetSpriteMaterial()
    {
        if (cachedSpriteMat == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
                cachedSpriteMat = new Material(shader);
        }
        return cachedSpriteMat;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore all trigger colliders (traps, doors, keys, light sources, etc.)
        if (other.isTrigger)
            return;

        // Player's bullet hits enemy body
        if (CompareTag("Bullet") && other.CompareTag("Enemy"))
        {
            other.GetComponent<EnemyHealth>()?.TakeDamage(1, RunKillAnalytics.DamageMethodBullet);
            Destroy(gameObject);
            return;
        }

        // Enemy's bullet hits player body
        if (CompareTag("EnemyBullet") && other.CompareTag("Player"))
        {
            other.GetComponent<PlayerHealth>()?.TakeDamage(1);
            Destroy(gameObject);
            return;
        }

        // Player bullets must ignore the player's own solid collider
        if (CompareTag("Bullet") && other.CompareTag("Player"))
            return;

        // Enemy bullets must ignore enemy solid colliders
        if (CompareTag("EnemyBullet") && other.CompareTag("Enemy"))
            return;

        // Walls / solid environment — offset light away from wall surface
        if (impactEchoPrefab != null)
        {
            Vector2 closestPoint = other.ClosestPoint(transform.position);
            Vector2 normal = ((Vector2)transform.position - closestPoint).normalized;
            // If bullet is exactly on the surface, fall back to velocity direction
            if (normal.sqrMagnitude < 0.01f && rb != null)
                normal = -rb.linearVelocity.normalized;
            Vector3 spawnPos = (Vector3)closestPoint + (Vector3)(normal * 0.6f);
            Instantiate(impactEchoPrefab, spawnPos, Quaternion.identity);
        }

        Destroy(gameObject);
    }

}
