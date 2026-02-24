using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 2;
    public GameObject hitGlowPrefab;

    public static System.Action OnEnemyKilled;

    [HideInInspector] public int currentHealth;
    private bool isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        currentHealth -= dmg;

        if (hitGlowPrefab != null)
        {
            Instantiate(hitGlowPrefab, transform.position, Quaternion.identity);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        OnEnemyKilled?.Invoke();
        GameManager.Instance?.EnemyKilled();
        Destroy(gameObject);
    }
}