using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 2;
    public GameObject hitGlowPrefab;
    public float healthBarDamageRevealSeconds = 2f;

    public static System.Action OnEnemyKilled;

    [HideInInspector] public int currentHealth;
    private bool isDead = false;
    private SpriteRenderer sr;
    private EnemyHealthBar healthBar;
    private EnemyAI ai;
    private float damageRevealTimer = 0f;
    private float litBufferTimer = 0f;
    private const float LitBufferSeconds = 0.12f;

    void Awake()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        ai = GetComponent<EnemyAI>();

        // Attach a mini health bar above this enemy
        healthBar = EnemyHealthBar.AttachTo(gameObject, maxHealth);
        healthBar?.SetVisible(false, true);
    }

    void Update()
    {
        if (isDead || healthBar == null) return;

        if (damageRevealTimer > 0f)
            damageRevealTimer -= Time.deltaTime;

        if (ai != null && ai.IsLit())
            litBufferTimer = LitBufferSeconds;
        else if (litBufferTimer > 0f)
            litBufferTimer -= Time.deltaTime;

        bool shouldShowBar = damageRevealTimer > 0f || litBufferTimer > 0f;
        healthBar.SetVisible(shouldShowBar);
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        currentHealth -= dmg;
        if (currentHealth < 0) currentHealth = 0;

        // Floating damage number above the enemy
        DamageNumber.Spawn(dmg, transform.position);

        // Update health bar
        healthBar?.SetFill(currentHealth, maxHealth);
        damageRevealTimer = healthBarDamageRevealSeconds;

        if (hitGlowPrefab != null)
        {
            var glow = Instantiate(hitGlowPrefab, transform.position, Quaternion.identity);
            glow.GetComponent<EnemyHitGlow>()?.TriggerGlow();
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
        // Disable AI, shooting, and collider so the enemy stops moving/dealing damage
        var ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;
        var shooting = GetComponent<EnemyShooting>();
        if (shooting != null) shooting.enabled = false;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        float duration = 0.3f;
        float elapsed = 0f;
        Color startColor = sr != null ? sr.color : Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (sr != null)
            {
                var c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
                sr.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
        StatusHUD.Instance?.DecrementEnemies();
    }
}