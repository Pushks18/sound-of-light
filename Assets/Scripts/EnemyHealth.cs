using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 2;
    public GameObject hitGlowPrefab;

    public static System.Action OnEnemyKilled;

    [HideInInspector] public int currentHealth;
    private bool isDead = false;
    private SpriteRenderer sr;

    void Awake()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        currentHealth -= dmg;
        if (currentHealth < 0) currentHealth = 0;

        if (hitGlowPrefab != null)
        {
            Instantiate(hitGlowPrefab, transform.position, Quaternion.identity);
        }

        UpdateOpacity();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateOpacity()
    {
        if (sr == null) return;

        // Map remaining HP to opacity: full HP = 1.0, 0 HP = 0.0
        float t = (float)currentHealth / maxHealth;
        // Curve so the fade is more dramatic: 1.0 → 0.7 → 0.35 → 0.0
        float alpha = t * t;
        var c = sr.color;
        c.a = Mathf.Clamp01(alpha);
        sr.color = c;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        OnEnemyKilled?.Invoke();
        GameManager.Instance?.EnemyKilled();

        // Quick fade-out then destroy
        StartCoroutine(DeathFade());
    }

    IEnumerator DeathFade()
    {
        // Disable AI and collider so the enemy stops moving/dealing damage
        var ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        float duration = 0.3f;
        float elapsed = 0f;
        Color startColor = sr != null ? sr.color : Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (sr != null)
            {
                var c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
                sr.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}