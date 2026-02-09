using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public float stopDistance = 1.2f; // pixels / units away from player

    private Transform player;
    private Rigidbody2D rb;

    private int lightCount = 0; // how many lights are touching enemy

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void FixedUpdate()
    {
        if (player == null || lightCount <= 0)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        // Move only until close enough
        if (distance > stopDistance)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            rb.velocity = dir * moveSpeed;
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("LightSource"))
        {
            lightCount++;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("LightSource"))
        {
            lightCount = Mathf.Max(0, lightCount - 1);
        }
    }
}