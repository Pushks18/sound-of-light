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
    private int lightCount = 0;         // how many lights currently touching

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
        markLight.lightType = Light2D.LightType.Freeform;
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
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > stopDistance)
        {
            Vector2 dir = (player.position - transform.position).normalized;

            //  Speed boost if currently in light
            float currentSpeed = (lightCount > 0) ? boostedSpeed : baseSpeed;

            rb.linearVelocity = dir * currentSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("LightSource"))
        {
            lightCount++;
            activated = true;   // permanently activate once lit
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("LightSource"))
        {
            lightCount = Mathf.Max(0, lightCount - 1);
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
        return lightCount > 0;
    }
}