using UnityEngine;
using UnityEngine.Rendering.Universal;
public class PlayerSlash : MonoBehaviour
{
    [Header("Slash Settings")]
    public float slashRadius = 2.5f;
    public float slashAngle = 120f;
    public float slashDuration = 0.3f;
    public int slashDamage = 2;
    public float energyCost = 3f;
    public float cooldown = 0.15f;

    [Header("Light Settings")]
    public float lightIntensity = 1.5f;
    public Color lightColor = new Color(1f, 0.9f, 0.6f);

    [Header("Visual Arc")]
    public Color arcColor = new Color(1f, 0.9f, 0.4f, 0.5f);
    public Color arcEdgeColor = new Color(1f, 1f, 0.8f, 0.8f);

    private PlayerMovement playerMovement;
    private LightEnergy lightEnergy;
    private float cooldownTimer;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        lightEnergy = GetComponent<LightEnergy>();
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.J) && cooldownTimer <= 0f)
        {
            if (lightEnergy != null && lightEnergy.TrySpend(energyCost))
            {
                PerformSlash();
                cooldownTimer = cooldown;
            }
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
        meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
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
        line.material = new Material(Shader.Find("Sprites/Default"));
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
