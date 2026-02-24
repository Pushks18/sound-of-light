using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement")]
    public float baseSpeed = 2f;
    public float boostedSpeed = 8f;
    public float stopDistance = 1.2f;

    private Transform player;
    private Rigidbody2D rb;

    private bool activated = false;     // permanently activated once lit
    private bool isCurrentlyLit = false; // true while any light source overlaps

    private float stunTimer;
    private float markTimer;

    private Light2D markLight;

    public bool IsActivated => activated;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

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
            // Reset lit flag — OnTriggerStay2D will set it again if still in light
            isCurrentlyLit = false;
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > stopDistance)
        {
            Vector2 dir = (player.position - transform.position).normalized;

            // Speed boost if currently in light
            float currentSpeed = isCurrentlyLit ? boostedSpeed : baseSpeed;

            rb.linearVelocity = dir * currentSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Reset each physics step — OnTriggerStay2D will re-set if still in light
        isCurrentlyLit = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("LightSource"))
        {
            isCurrentlyLit = true;
            activated = true;   // permanently activate once lit
        }
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
}