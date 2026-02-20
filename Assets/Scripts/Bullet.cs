using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public GameObject impactEchoPrefab;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = transform.up * speed;

        // optional safety: ignore the shooter if you want
        // Physics2D.IgnoreCollision(...)

        Destroy(gameObject, 3f);
    }

    // If your bullets are set to be Triggers (IsTrigger = true)
    void OnTriggerEnter2D(Collider2D other)
    {
        // ignore sensors
        if (other.CompareTag("EnemyRange") || other.CompareTag("LightSource"))
            return;

        // Player's bullet hits enemy body
        if (CompareTag("Bullet") && other.CompareTag("Enemy"))
        {
            other.GetComponent<EnemyHealth>()?.TakeDamage(1);
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

        // walls / environment
        if (impactEchoPrefab != null)
            Instantiate(impactEchoPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

}