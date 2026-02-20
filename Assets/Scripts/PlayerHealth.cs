using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    [HideInInspector] public int currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int dmg)
    {
        currentHealth -= dmg;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    void Die()
    {
        var deathScreen = FindAnyObjectByType<DeathScreen>();
        if (deathScreen != null)
        {
            deathScreen.Show();
            // Disable player visuals and input without deactivating the
            // GameObject, so the child camera keeps rendering.
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            foreach (var mb in GetComponents<MonoBehaviour>())
            {
                if (mb != this) mb.enabled = false;
            }
            enabled = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
