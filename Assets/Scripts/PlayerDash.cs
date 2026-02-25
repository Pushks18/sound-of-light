using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class PlayerDash : MonoBehaviour
{
    [Header("Trail Lights")]
    public float trailSpawnInterval = 0.03f;
    public float trailLightRadius = 0.8f;
    public float trailLightIntensity = 0.8f;
    public float trailLightDuration = 0.6f;
    public Color trailColor = new Color(0.6f, 0.8f, 1f);

    [Header("Shadow Afterimage")]
    public float shadowDuration = 0.4f;
    public Color shadowColor = new Color(0.4f, 0.6f, 1f, 0.6f);
    public float shadowSpawnInterval = 0.04f;

    [Header("Contact Damage")]
    public float stunDuration = 0.2f;

    // Not serialized — avoids Unity overriding with old saved values
    private const int contactDamage = 2;
    private const float contactRadius = 2f;

    private PlayerMovement playerMovement;
    private SpriteRenderer playerSprite;
    private float trailSpawnTimer;
    private float shadowSpawnTimer;
    private HashSet<int> hitEnemiesThisDash;
    private Vector3 lastDashPos;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerSprite = GetComponent<SpriteRenderer>();
        hitEnemiesThisDash = new HashSet<int>();

        if (playerMovement != null)
            playerMovement.OnDashStart += OnDashStart;
    }

    void OnDestroy()
    {
        if (playerMovement != null)
            playerMovement.OnDashStart -= OnDashStart;
    }

    void OnDashStart()
    {
        hitEnemiesThisDash.Clear();
        trailSpawnTimer = 0f;
        shadowSpawnTimer = 0f;
        lastDashPos = transform.position;

        // Immediately check for enemies at the start position
        CheckContactDamage();
    }

    void Update()
    {
        if (playerMovement == null || !playerMovement.IsDashing)
            return;

        trailSpawnTimer -= Time.deltaTime;
        if (trailSpawnTimer <= 0f)
        {
            SpawnTrailLight();
            trailSpawnTimer = trailSpawnInterval;
        }

        shadowSpawnTimer -= Time.deltaTime;
        if (shadowSpawnTimer <= 0f)
        {
            SpawnShadowAfterimage();
            shadowSpawnTimer = shadowSpawnInterval;
        }

        CheckContactDamage();
    }

    void SpawnTrailLight()
    {
        var trailObj = new GameObject("DashTrailLight");
        trailObj.transform.position = transform.position;
        trailObj.tag = "LightSource";

        var light = trailObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = trailColor;
        light.intensity = trailLightIntensity;
        light.pointLightOuterRadius = trailLightRadius;
        light.pointLightInnerRadius = trailLightRadius * 0.3f;
        light.pointLightOuterAngle = 360f;
        light.pointLightInnerAngle = 360f;
        light.shadowsEnabled = false;

        var collider = trailObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = trailLightRadius;

        var fader = trailObj.AddComponent<DashTrailFader>();
        fader.duration = trailLightDuration;
        fader.startIntensity = trailLightIntensity;
        fader.startRadius = trailLightRadius;
    }

    void SpawnShadowAfterimage()
    {
        if (playerSprite == null || playerSprite.sprite == null)
            return;

        var shadowObj = new GameObject("DashShadow");
        shadowObj.transform.position = transform.position;
        shadowObj.transform.rotation = transform.rotation;
        shadowObj.transform.localScale = transform.lossyScale;

        var sr = shadowObj.AddComponent<SpriteRenderer>();
        sr.sprite = playerSprite.sprite;
        sr.color = shadowColor;
        sr.sortingLayerID = playerSprite.sortingLayerID;
        sr.sortingOrder = playerSprite.sortingOrder - 1;
        sr.material = playerSprite.material;

        var fader = shadowObj.AddComponent<ShadowAfterImageFader>();
        fader.duration = shadowDuration;
        fader.startAlpha = shadowColor.a;
    }

    void CheckContactDamage()
    {
        Vector2 currentPos = transform.position;
        Vector2 prevPos = lastDashPos;

        // Check all enemies by direct distance (bypasses layer/physics issues)
        var enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            int id = enemy.gameObject.GetInstanceID();
            if (hitEnemiesThisDash.Contains(id)) continue;

            Vector2 enemyPos = enemy.transform.position;

            // Check distance to current position OR closest point on dash path
            float dist = DistToSegment(enemyPos, prevPos, currentPos);
            Debug.Log($"[Dash] Enemy {enemy.name} dist={dist:F2}, radius={contactRadius}");
            if (dist <= contactRadius)
            {
                Debug.Log($"[Dash] HIT {enemy.name} for {contactDamage} damage!");
                hitEnemiesThisDash.Add(id);
                enemy.TakeDamage(contactDamage);

                var ai = enemy.GetComponent<EnemyAI>();
                if (ai != null) ai.Stun(stunDuration);
            }
        }

        // Check all enemy bullets by direct distance
        var bullets = FindObjectsByType<Bullet>(FindObjectsSortMode.None);
        foreach (var bullet in bullets)
        {
            if (bullet == null || !bullet.CompareTag("EnemyBullet")) continue;

            Vector2 bulletPos = bullet.transform.position;
            float dist = DistToSegment(bulletPos, prevPos, currentPos);
            if (dist <= contactRadius)
                Destroy(bullet.gameObject);
        }

        lastDashPos = transform.position;
    }

    // Shortest distance from point P to line segment AB
    float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float sqrLen = ab.sqrMagnitude;

        // a and b are the same point
        if (sqrLen < 0.0001f)
            return Vector2.Distance(p, a);

        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / sqrLen);
        Vector2 closest = a + t * ab;
        return Vector2.Distance(p, closest);
    }
}

public class DashTrailFader : MonoBehaviour
{
    [HideInInspector] public float duration;
    [HideInInspector] public float startIntensity;
    [HideInInspector] public float startRadius;

    private Light2D light2D;
    private float elapsed;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = 1f - (elapsed / duration);

        if (light2D != null)
        {
            light2D.intensity = startIntensity * t;
            light2D.pointLightOuterRadius = startRadius * (0.3f + 0.7f * t);
        }

        if (elapsed >= duration)
        {
            // Disable collider before Destroy so OnTriggerExit2D fires on enemies
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            Destroy(gameObject);
        }
    }
}

public class ShadowAfterImageFader : MonoBehaviour
{
    [HideInInspector] public float duration;
    [HideInInspector] public float startAlpha;

    private SpriteRenderer sr;
    private float elapsed;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = 1f - (elapsed / duration);

        if (sr != null)
        {
            var c = sr.color;
            c.a = startAlpha * t;
            sr.color = c;
        }

        if (elapsed >= duration)
            Destroy(gameObject);
    }
}
