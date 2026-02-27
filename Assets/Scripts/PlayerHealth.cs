using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    [HideInInspector] public int currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
        // Initialise HUD with max HP
        StatusHUD.Instance?.UpdateHP(currentHealth, maxHealth);
    }

    public void TakeDamage(int dmg)
    {
        currentHealth -= dmg;

        // 🔥 Tutorial safety: never go below 1 HP
        if (SceneManager.GetActiveScene().name == "TutorialScene")
        {
            if (currentHealth < 1)
            {
                currentHealth = 1;
            }
        }

        GameUIManager.Instance?.UpdateHP(currentHealth);
        StatusHUD.Instance?.UpdateHP(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
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