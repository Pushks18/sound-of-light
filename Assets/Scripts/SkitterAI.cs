using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class SkitterAI : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField] private float triTipY  =  1.1f;
    [SerializeField] private float triBaseX =  0.9f;
    [SerializeField] private float triBaseY = -0.9f;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float panicSpeed = 6f;
    public float fleeRange = 4f;
    public float preferredDistance = 6f;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public float shootInterval = 3f;
    public float shootRange = 12f;

    [Header("Light Reaction")]
    public float lightDetectRadius = 5f;

    [Header("Detection")]
    public float playerDetectRange = 15f;

    [Header("Wall Avoidance")]
    public float wallCheckDist = 1.4f;
    public float wallRepulsionStrength = 3f;

    private Transform player;
    private Rigidbody2D rb;
    private float shootTimer;
    private bool activated;
    private Light2D bodyLight;
    [HideInInspector] public MeshRenderer meshRend;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearDamping = 3f;
        rb.mass = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        BuildTriangleVisual();
        BuildBodyLight();
        BuildDetectionTrigger();
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        shootTimer = shootInterval;
        StatusHUD.Instance?.UpdateEnemies();
    }

    void BuildTriangleVisual()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box != null) Destroy(box);

        var poly = GetComponent<PolygonCollider2D>();
        if (poly == null) poly = gameObject.AddComponent<PolygonCollider2D>();
        poly.isTrigger = false;
        poly.SetPath(0, new Vector2[]
        {
            new Vector2(0f,        triTipY),
            new Vector2(-triBaseX, triBaseY),
            new Vector2( triBaseX, triBaseY)
        });

        var mf = gameObject.AddComponent<MeshFilter>();
        meshRend = gameObject.AddComponent<MeshRenderer>();

        var mesh = new Mesh { name = "SkitterTriangle" };
        mesh.vertices = new Vector3[]
        {
            new Vector3(0f,        triTipY,  0f),
            new Vector3(-triBaseX, triBaseY, 0f),
            new Vector3( triBaseX, triBaseY, 0f)
        };
        mesh.triangles = new int[] { 0, 2, 1 };
        mesh.uv = new Vector2[] { new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f) };
        mesh.RecalculateNormals();
        mf.mesh = mesh;

        var mat = new Material(Shader.Find("Sprites/Default"))
        {
            color = new Color(0.95f, 0.15f, 0.15f)
        };
        meshRend.material = mat;
        meshRend.sortingOrder = 10;
    }

    void BuildBodyLight()
    {
        bodyLight = gameObject.AddComponent<Light2D>();
        bodyLight.lightType = Light2D.LightType.Point;
        bodyLight.color = new Color(0.95f, 0.15f, 0.15f);
        bodyLight.intensity = 0f;
        bodyLight.pointLightOuterRadius = 2.5f;
        bodyLight.pointLightInnerRadius = 0.3f;
        bodyLight.shadowsEnabled = false;
    }

    void BuildDetectionTrigger()
    {
        var detObj = new GameObject("SkitterSensor");
        detObj.transform.SetParent(transform, false);
        detObj.layer = gameObject.layer;
        var krb = detObj.AddComponent<Rigidbody2D>();
        krb.bodyType = RigidbodyType2D.Kinematic;
        var circle = detObj.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = lightDetectRadius;
        detObj.AddComponent<SkitterLightSensor>().owner = this;
    }

    public void NotifyLightNearby()
    {
        if (!activated)
        {
            activated = true;
            if (bodyLight != null) bodyLight.intensity = 0.8f;
        }
    }

    void Update()
    {
        // Activate when player walks into detection range, even without light
        if (!activated && player != null &&
            Vector2.Distance(transform.position, player.position) <= playerDetectRange)
        {
            NotifyLightNearby();
        }

        if (!activated || player == null) return;

        // Rotate tip to face player
        Vector2 dir = (Vector2)player.position - (Vector2)transform.position;
        if (dir.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.Euler(0f, 0f, angle), 10f * Time.deltaTime);
        }

        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f &&
            Vector2.Distance(transform.position, player.position) <= shootRange)
        {
            Shoot();
            shootTimer = shootInterval;
        }
    }

    void FixedUpdate()
    {
        if (player == null || !activated)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // --- Flee from nearby LightSource triggers ---
        var hits = Physics2D.OverlapCircleAll(transform.position, lightDetectRadius);
        Vector2 fleeFromLight = Vector2.zero;
        bool frightened = false;

        foreach (var col in hits)
        {
            if (!col.isTrigger) continue;
            if (!col.CompareTag("LightSource")) continue;
            if (col.transform.IsChildOf(transform)) continue;

            Vector2 away = (Vector2)transform.position - (Vector2)col.transform.position;
            float d = away.magnitude;
            if (d < 0.01f) continue;

            fleeFromLight += away.normalized * (lightDetectRadius / Mathf.Max(d, 0.3f));
            frightened = true;
        }

        if (frightened)
        {
            rb.linearVelocity = fleeFromLight.normalized * panicSpeed;
            return;
        }

        // --- Distance-keeping vs player with corner escape ---
        float playerDist = Vector2.Distance(transform.position, player.position);

        if (playerDist < fleeRange)
        {
            Vector2 awayFromPlayer = ((Vector2)transform.position - (Vector2)player.position).normalized;
            Vector2 wallRepulsion  = ComputeWallRepulsion(wallCheckDist);

            Vector2 moveDir = awayFromPlayer + wallRepulsion * wallRepulsionStrength;

            if (moveDir.sqrMagnitude < 0.05f)
            {
                // Forces cancelled out — strafe perpendicular to escape corner
                moveDir = new Vector2(-awayFromPlayer.y, awayFromPlayer.x);
            }

            rb.linearVelocity = moveDir.normalized * panicSpeed;
        }
        else if (playerDist > preferredDistance + 1f)
        {
            Vector2 toward = ((Vector2)player.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = toward * (moveSpeed * 0.5f);
        }
        else
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 5f * Time.fixedDeltaTime);
        }
    }

    // 8-direction raycasts; walls push back proportionally to proximity
    Vector2 ComputeWallRepulsion(float checkDist)
    {
        Vector2 result = Vector2.zero;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, checkDist);
            if (hit.collider == null) continue;
            if (hit.collider.isTrigger) continue;
            if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player")) continue;

            float weight = 1f - (hit.distance / checkDist);
            result -= dir * weight;
        }
        return result;
    }

    void Shoot()
    {
        if (bulletPrefab == null || player == null) return;
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        Instantiate(bulletPrefab, transform.position, Quaternion.Euler(0f, 0f, angle));
    }

    public void DisableForDeath()
    {
        enabled = false;
        rb.linearVelocity = Vector2.zero;
    }

    public IEnumerator FadeOut(float duration)
    {
        if (meshRend == null) yield break;
        Color startColor = meshRend.material.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            meshRend.material.color = c;
            if (bodyLight != null)
                bodyLight.intensity = Mathf.Lerp(0.8f, 0f, t);
            yield return null;
        }
    }

    public bool IsActivated => activated;
    public bool IsLit() => false;
    public bool IsStunned() => false;
}
