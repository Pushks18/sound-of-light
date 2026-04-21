using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement")]
    public float baseSpeed = 2f;
    public float boostedSpeed = 8f;
    public float stopDistance = 1.2f;

    [Header("Hunt Mode")]
    public float pathRecalcInterval = 0.7f;

    private Transform player;
    private Rigidbody2D rb;

    private bool activated = false;     // permanently activated once lit
    private bool isCurrentlyLit = false; // true while any light source overlaps

    private float stunTimer;
    private float markTimer;

    private Light2D markLight;

    // Hunt-mode pathfinding state
    private bool hunting;
    private TilemapRoomBuilder cachedBuilder;
    private List<Vector2Int> currentPath;
    private int pathIndex;
    private float pathRecalcTimer;

    // Stuck detection
    private Vector2 lastProgressPos;
    private float stuckTimer;
    private const float StuckMoveDist = 0.2f;
    private const float StuckRecalcTime = 0.25f;

    public bool IsActivated => activated;

    void Start()
    {
        EnemyRegistry.Register(this);

        rb = GetComponent<Rigidbody2D>();
        rb.mass = 100f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Ensure the main collider is solid so the player can't walk through
        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = false;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        lastProgressPos = transform.position;

        // Mark glow (small red light when hit)
        var markObj = new GameObject("MarkLight");
        markObj.transform.SetParent(transform);
        markObj.transform.localPosition = Vector3.zero;

        markLight = markObj.AddComponent<Light2D>();
        markLight.lightType = Light2D.LightType.Point;
        markLight.pointLightOuterRadius = 1.5f;
        markLight.pointLightInnerRadius = 0.5f;
        markLight.intensity = 0.6f;
        markLight.falloffIntensity = 0.8f;
        markLight.color = new Color(1f, 0.3f, 0.2f);
        markLight.shadowsEnabled = false;
        markLight.enabled = false;
        
        StatusHUD.Instance?.UpdateEnemies();
    }

    void Update()
    {
        if (stunTimer > 0f)
            stunTimer -= Time.deltaTime;

        if (markTimer > 0f)
        {
            markTimer -= Time.deltaTime;
            if (!markLight.enabled)
                markLight.enabled = true;
        }
        else if (markLight.enabled)
        {
            markLight.enabled = false;
        }
    }

    void FixedUpdate()
    {
        if (player == null || !activated || stunTimer > 0f)
        {
            rb.linearVelocity = Vector2.zero;
            isCurrentlyLit = false;
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;
            isCurrentlyLit = false;
            return;
        }

        float currentSpeed = isCurrentlyLit ? boostedSpeed : baseSpeed;

        if (hunting && cachedBuilder != null)
        {
            FollowPath(currentSpeed);
        }
        else
        {
            Vector2 dir = (player.position - transform.position).normalized;
            rb.linearVelocity = dir * currentSpeed;
        }

        // Reset each physics step — OnTriggerStay2D will re-set if still in light
        isCurrentlyLit = false;
    }

    void FollowPath(float speed)
    {
        pathRecalcTimer -= Time.fixedDeltaTime;

        // Stuck detection — if barely moved, force immediate recalc
        float moved = Vector2.Distance(transform.position, lastProgressPos);
        if (moved < StuckMoveDist)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= StuckRecalcTime)
            {
                pathRecalcTimer = -1f; // force recalc now
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
            lastProgressPos = transform.position;
        }

        if (pathRecalcTimer <= 0f || currentPath == null)
        {
            RecalculatePath();
            pathRecalcTimer = pathRecalcInterval;
        }

        // No valid path — stop instead of pushing into walls
        if (currentPath == null || pathIndex >= currentPath.Count)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector3 waypoint = cachedBuilder.CellToWorld(currentPath[pathIndex]);

        // Advance past waypoints we're already close to (0.7 for smoother turning)
        while (pathIndex < currentPath.Count - 1 &&
               Vector2.Distance(transform.position, waypoint) < 0.7f)
        {
            pathIndex++;
            waypoint = cachedBuilder.CellToWorld(currentPath[pathIndex]);
        }

        Vector2 moveDir = ((Vector2)waypoint - (Vector2)transform.position).normalized;
        rb.linearVelocity = moveDir * speed;
    }

    void RecalculatePath()
    {
        if (cachedBuilder == null || player == null) return;

        Vector2Int startCell = cachedBuilder.WorldToCell(transform.position);
        Vector2Int goalCell = cachedBuilder.WorldToCell(player.position);

        // Try padded path first so the enemy doesn't clip wall corners
        currentPath = GridPathfinder.FindPath(startCell, goalCell, cachedBuilder.IsFloorPadded);

        // Fall back to unpadded if no wide path exists
        if (currentPath == null)
            currentPath = GridPathfinder.FindPath(startCell, goalCell, cachedBuilder.IsFloor);

        pathIndex = 1; // skip the cell we're already standing on
    }

    /// <summary>
    /// Forces this enemy into hunt mode: activates immediately and
    /// navigates around walls using A* pathfinding.
    /// </summary>
    public void ActivateHunt()
    {
        if (hunting) return;

        activated = true;
        hunting = true;

        if (cachedBuilder == null)
            cachedBuilder = Object.FindAnyObjectByType<TilemapRoomBuilder>();

        // Recalculate path immediately
        RecalculatePath();
        pathRecalcTimer = pathRecalcInterval;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("LightSource"))
        {
            isCurrentlyLit = true;

            if (!activated)
            {
                activated = true;
                // Enable A* pathfinding on first activation
                if (!hunting)
                {
                    hunting = true;
                    if (cachedBuilder == null)
                        cachedBuilder = Object.FindAnyObjectByType<TilemapRoomBuilder>();
                    RecalculatePath();
                    pathRecalcTimer = pathRecalcInterval;
                }
            }
        }
    }

    void OnDestroy()
    {
        EnemyRegistry.Unregister(this);
    }

    public void Stun(float duration)
    {
        stunTimer = duration;
        rb.linearVelocity = Vector2.zero;
    }

    public void Mark(float duration)
    {
        markTimer = duration;
    }

    public bool IsStunned()
    {
        return stunTimer > 0f;
    }

    public bool IsLit()
    {
        return isCurrentlyLit;
    }

    public bool IsHunting()
    {
        return hunting;
    }
}
