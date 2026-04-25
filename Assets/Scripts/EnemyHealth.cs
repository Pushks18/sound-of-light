using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 2;
    [Header("Damage Modifiers")]
    [Tooltip("Multiplier applied to incoming slash damage. 1 = full damage, 0.5 = half damage.")]
    public float slashDamageMultiplier = 1f;

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
    private string lastDamageMethod = RunKillAnalytics.DamageMethodUnknown;
    private bool hasTakenTrackedDamage = false;
    private float firstDamageTimestamp = 0f;
    private readonly HashSet<string> damageTypesUsed = new HashSet<string>();

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

    public void TakeDamage(int dmg, string damageMethod = RunKillAnalytics.DamageMethodUnknown)
    {
        if (isDead) return;

        lastDamageMethod = NormalizeDamageMethod(damageMethod);

        if (!hasTakenTrackedDamage)
        {
            hasTakenTrackedDamage = true;
            firstDamageTimestamp = Time.time;
        }

        damageTypesUsed.Add(lastDamageMethod);

        int finalDmg = dmg;
        if (lastDamageMethod == RunKillAnalytics.DamageMethodSlash)
            finalDmg = Mathf.Max(1, Mathf.RoundToInt(dmg * slashDamageMultiplier));
        currentHealth -= finalDmg;
        if (currentHealth < 0) currentHealth = 0;

        // Floating damage number above the enemy
        DamageNumber.Spawn(finalDmg, transform.position);

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
        float alpha = t * t;
        var c = sr.color;
        c.a = Mathf.Clamp01(alpha);
        sr.color = c;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        float timeToKillSeconds = hasTakenTrackedDamage ? Mathf.Max(0f, Time.time - firstDamageTimestamp) : 0f;
        RunKillAnalytics.Instance?.RecordEnemyTimeToKill(gameObject.name, timeToKillSeconds, damageTypesUsed, lastDamageMethod);
        RunKillAnalytics.Instance?.RecordEnemyKill(lastDamageMethod);
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
        var skitter = GetComponent<SkitterAI>();
        if (skitter != null) skitter.DisableForDeath();
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        float duration = 0.3f;

        if (skitter != null)
        {
            yield return skitter.FadeOut(duration);
        }
        else
        {
            float elapsed = 0f;
            Color startColor = sr != null ? sr.color : Color.white;
            MeshRenderer mr = sr == null ? GetComponent<MeshRenderer>() : null;
            if (mr != null) startColor = mr.material.color;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (sr != null)
                {
                    var c = startColor;
                    c.a = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
                    sr.color = c;
                }
                else if (mr != null)
                {
                    var c = startColor;
                    c.a = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
                    mr.material.color = c;
                }
                yield return null;
            }
        }

        Destroy(gameObject);
        StatusHUD.Instance?.DecrementEnemies();
    }

    string NormalizeDamageMethod(string damageMethod)
    {
        if (string.IsNullOrWhiteSpace(damageMethod))
            return RunKillAnalytics.DamageMethodUnknown;

        return damageMethod.Trim().ToLowerInvariant();
    }
}
