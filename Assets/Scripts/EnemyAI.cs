using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EnemyAI : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public float stopDistance = 1.2f;

    private Transform player;
    private Rigidbody2D rb;

    private bool activated = false;
    private float stunTimer;
    private float markTimer;
    private Light2D markLight;

    public bool IsActivated => activated;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Small light that reveals this enemy when marked
        var markObj = new GameObject("MarkLight");
        markObj.transform.SetParent(transform);
        markObj.transform.localPosition = Vector3.zero;
        markLight = markObj.AddComponent<Light2D>();
        markLight.lightType = Light2D.LightType.Point;
        markLight.pointLightOuterRadius = 1.2f;
        markLight.pointLightInnerRadius = 0.3f;
        markLight.intensity = 0.6f;
        markLight.color = new Color(1f, 0.3f, 0.2f); // red-orange glow
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
            rb.linearVelocity = dir * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("LightSource"))
            activated = true;
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
        return activated;
    }
}
