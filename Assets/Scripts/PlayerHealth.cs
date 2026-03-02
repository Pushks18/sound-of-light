using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    [HideInInspector] public int currentHealth;

    [Header("Damage Flash Settings")]
    [SerializeField]
    private float flashDuration = 0.08f;
    [SerializeField]
    private Color flashColor = Color.red;
    private SpriteRenderer sr;
    private Color originalColor;
    private bool isFlashing = false;
    private bool isDead = false;

    [Header("Invincibility")]
    [SerializeField] private float iFrameDuration = 0.5f;
    private float iFrameTimer = 0f;

    void Awake()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            originalColor = sr.color;

        // Initialise HUD with max HP
        StatusHUD.Instance?.UpdateHP(currentHealth, maxHealth);
    }

    void Update()
    {
        if (iFrameTimer > 0f)
            iFrameTimer -= Time.deltaTime;
    }

    public void TakeDamage(int dmg)
    {
        if (isDead || iFrameTimer > 0f) return;
        if (GameManager.Instance != null && GameManager.Instance.gameEnded) return;

        iFrameTimer = iFrameDuration;
        currentHealth -= dmg;

        // 🔥 Tutorial safety: never go below 1 HP
        /*if (SceneManager.GetActiveScene().name == "TutorialScene")
        {
            if (currentHealth < 1)
            {
                currentHealth = 1;
            }
        }*/

        GameUIManager.Instance?.UpdateHP(currentHealth);
        StatusHUD.Instance?.UpdateHP(currentHealth, maxHealth);

        // Flash red when hit
        DamageNumber.Spawn(dmg, transform.position);
        if (!isFlashing)
            StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator DamageFlash()
    {
        if (sr == null) yield break;

        isFlashing = true;

        int flashCount = 3;

        for (int i = 0; i < flashCount; i++)
        {
            sr.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            sr.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }

        isFlashing = false;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();
        if (sr != null) sr.color = originalColor;
        isFlashing = false;

        GameManager.Instance?.PlayerDied();

        var deathScreen = FindAnyObjectByType<DeathScreen>();
        if (deathScreen != null)
        {
            deathScreen.Show();

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