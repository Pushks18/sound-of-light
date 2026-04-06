using UnityEngine;
using UnityEngine.Rendering.Universal;
public class PlayerSlash : MonoBehaviour
{
    [Header("Slash Settings")]
    public float slashRadius = 2.5f;
    public float slashAngle = 120f;
    public float slashDuration = 0.3f;
    public int slashDamage = 2;
    public float cooldown = 0.15f;

    [Header("Light Settings")]
    public float lightIntensity = 1.5f;
    public Color lightColor = new Color(1f, 0.9f, 0.6f);

    [Header("Visual Arc")]
    public Color arcColor = new Color(1f, 0.9f, 0.4f, 0.5f);
    public Color arcEdgeColor = new Color(1f, 1f, 0.8f, 0.8f);

    private PlayerMovement playerMovement;
    private float cooldownTimer;

    // Cached shared resources to avoid per-use Shader.Find and Texture2D allocations
    private static Material cachedSpriteMat;
    private static Sprite   cachedCircleSprite;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        LevelExit exit = FindFirstObjectByType<LevelExit>();
        if (exit != null)
        {
            bool done = exit.GetLevelDone();
            if (done) return;
        }

        if (Input.GetKeyDown(KeyCode.J) && cooldownTimer <= 0f)
        {
            PerformSlash();
            cooldownTimer = cooldown;
        }
    }

    void PerformSlash()
    {
        Vector2 aim = playerMovement != null ? playerMovement.AimDirection : Vector2.up;
        float aimAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg - 90f;

        // Create slash object
        var slashObj = new GameObject("SlashLight");
        slashObj.transform.position = transform.position;
        slashObj.transform.rotation = Quaternion.Euler(0f, 0f, aimAngle);
        slashObj.tag = "LightSource";

        // Point Light2D as cone
        var light = slashObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = lightColor;
        light.intensity = lightIntensity;
        light.pointLightOuterRadius = slashRadius;
        light.pointLightInnerRadius = slashRadius * 0.25f;
        light.pointLightOuterAngle = slashAngle;
        light.pointLightInnerAngle = slashAngle * 0.5f;
        light.shadowsEnabled = true;
        light.falloffIntensity = 0.5f;

        // Trigger collider for enemy activation
        float halfAngleRad = slashAngle * 0.5f * Mathf.Deg2Rad;
        int segments = 16;
        var points = new Vector2[segments + 2];
        points[0] = Vector2.zero;
        for (int i = 0; i <= segments; i++)
        {
            float angle = -halfAngleRad + (2f * halfAngleRad * i / segments);
            points[i + 1] = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * slashRadius;
        }
        var collider = slashObj.AddComponent<PolygonCollider2D>();
        collider.isTrigger = true;
        collider.points = points;

        // Visual arc mesh
        CreateArcVisual(slashObj, halfAngleRad, segments, points);

        // Fade + destroy
        var fader = slashObj.AddComponent<SlashFader>();
        fader.duration = slashDuration;

        // Continuous bullet deflection for the slash's lifetime
        var deflector = slashObj.AddComponent<SlashBulletDeflector>();
        deflector.owner = this;

        // Damage enemies in arc
        DamageEnemiesInArc(aim);
    }

    void CreateArcVisual(GameObject parent, float halfAngleRad, int segments, Vector2[] arcPoints)
    {
        // --- Filled arc mesh ---
        var meshObj = new GameObject("SlashArcMesh");
        meshObj.transform.SetParent(parent.transform, false);

        var meshFilter = meshObj.AddComponent<MeshFilter>();
        var meshRenderer = meshObj.AddComponent<MeshRenderer>();
        meshRenderer.material = GetSpriteMaterial();
        meshRenderer.sortingOrder = 10;

        // Build fan mesh: center vertex + arc vertices
        int vertCount = segments + 2; // center + (segments+1) arc points
        var vertices = new Vector3[vertCount];
        var colors = new Color[vertCount];
        vertices[0] = Vector3.zero;
        colors[0] = arcColor;

        for (int i = 0; i <= segments; i++)
        {
            float angle = -halfAngleRad + (2f * halfAngleRad * i / segments);
            vertices[i + 1] = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f) * slashRadius;
            colors[i + 1] = arcEdgeColor;
        }

        // Triangles: fan from center
        int[] triangles = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        var mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        // --- Edge outline using LineRenderer ---
        var edgeObj = new GameObject("SlashArcEdge");
        edgeObj.transform.SetParent(parent.transform, false);

        var line = edgeObj.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.material = GetSpriteMaterial();
        line.startColor = arcEdgeColor;
        line.endColor = arcEdgeColor;
        line.startWidth = 0.06f;
        line.endWidth = 0.06f;
        line.sortingOrder = 11;

        // Outline: origin → arc edge → back to origin
        int linePoints = segments + 3;
        line.positionCount = linePoints;
        line.SetPosition(0, Vector3.zero);
        for (int i = 0; i <= segments; i++)
        {
            float angle = -halfAngleRad + (2f * halfAngleRad * i / segments);
            Vector3 p = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f) * slashRadius;
            line.SetPosition(i + 1, p);
        }
        line.SetPosition(linePoints - 1, Vector3.zero);
    }

    void DamageEnemiesInArc(Vector2 aim)
    {
        float halfAngle = slashAngle * 0.5f;
        var enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        foreach (var health in enemies)
        {
            Vector2 toEnemy = (Vector2)health.transform.position - (Vector2)transform.position;
            float distance = toEnemy.magnitude;

            if (distance > slashRadius)
                continue;

            // If enemy is extremely close, skip angle check (always hit)
            if (distance > 0.1f)
            {
                float angle = Vector2.Angle(aim, toEnemy);
                if (angle > halfAngle)
                    continue;
            }

            // Line-of-sight check — skip if a wall is blocking
            if (distance > 0.1f && IsBlockedByWall((Vector2)transform.position, (Vector2)health.transform.position, health.gameObject))
                continue;

            health.TakeDamage(slashDamage);
        }
    }

    public void SpawnDeflectSpark(Vector3 position)
    {
        var sparkObj = new GameObject("DeflectSpark");
        sparkObj.transform.position = position;

        // Bright flash light
        var light = sparkObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(0.8f, 0.9f, 1f);
        light.intensity = 2f;
        light.pointLightOuterRadius = 1.5f;
        light.pointLightInnerRadius = 0.3f;
        light.pointLightOuterAngle = 360f;
        light.pointLightInnerAngle = 360f;
        light.shadowsEnabled = false;

        // Small visible sprite (unlit so it's always visible)
        var sr = sparkObj.AddComponent<SpriteRenderer>();
        sr.sprite = GetCircleSprite();
        sr.material = GetSpriteMaterial();
        sr.color = new Color(0.8f, 0.9f, 1f, 0.9f);
        sr.sortingOrder = 12;
        sparkObj.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

        // Fade out and destroy
        var fader = sparkObj.AddComponent<DeflectSparkFader>();
        fader.duration = 0.2f;
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

    static Sprite GetCircleSprite()
    {
        if (cachedCircleSprite == null)
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size * 0.5f;
            float radius = center - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float a = Mathf.Clamp01(1f - (dist / radius));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }

            tex.Apply();
            cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
        return cachedCircleSprite;
    }

    bool IsBlockedByWall(Vector2 origin, Vector2 target, GameObject targetObj)
    {
        Vector2 dir = target - origin;
        float dist = dir.magnitude;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir.normalized, dist);

        foreach (var hit in hits)
        {
            if (hit.collider.isTrigger) continue;
            if (hit.collider.gameObject == gameObject) continue;   // skip player
            if (hit.collider.gameObject == targetObj) continue;    // skip the enemy
            // A solid non-trigger collider is between player and enemy — wall
            return true;
        }
        return false;
    }
}

public class SlashFader : MonoBehaviour
{
    [HideInInspector] public float duration = 0.3f;
    private float elapsed;

    private MeshRenderer meshRenderer;
    private LineRenderer lineRenderer;

    void Start()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        lineRenderer = GetComponentInChildren<LineRenderer>();
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = 1f - (elapsed / duration);

        if (meshRenderer != null)
        {
            var c = meshRenderer.material.color;
            c.a = t * 0.5f;
            meshRenderer.material.color = c;
        }

        if (lineRenderer != null)
        {
            var c = lineRenderer.startColor;
            c.a = t * 0.8f;
            lineRenderer.startColor = c;
            lineRenderer.endColor = c;
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

public class SlashBulletDeflector : MonoBehaviour
{
    [HideInInspector] public PlayerSlash owner;

    void OnTriggerEnter2D(Collider2D other)
    {
        // The slash's PolygonCollider2D overlaps with an enemy bullet
        var bullet = other.GetComponent<Bullet>();
        if (bullet != null && other.CompareTag("EnemyBullet"))
        {
            if (owner != null)
                owner.SpawnDeflectSpark(other.transform.position);
            Destroy(other.gameObject);
        }
    }
}

public class DeflectSparkFader : MonoBehaviour
{
    [HideInInspector] public float duration = 0.2f;
    private float elapsed;
    private Light2D light2D;
    private SpriteRenderer sr;

    void Start()
    {
        light2D = GetComponent<Light2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = 1f - (elapsed / duration);

        if (light2D != null)
            light2D.intensity = 2f * t;

        if (sr != null)
        {
            var c = sr.color;
            c.a = t;
            sr.color = c;
        }

        // Scale up slightly as it fades (expanding spark)
        float scale = Mathf.Lerp(0.3f, 0.6f, elapsed / duration);
        transform.localScale = new Vector3(scale, scale, 1f);

        if (elapsed >= duration)
            Destroy(gameObject);
    }
}
