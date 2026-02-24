using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 2;
    public GameObject hitGlowPrefab;

    public static System.Action OnEnemyKilled;

    [HideInInspector] public int currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int dmg)
    {
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
        // 🔥 Notify tutorial system
        OnEnemyKilled?.Invoke();

        // 🔥 Update enemy count safely
        GameUIManager.Instance?.UpdateEnemyCount(
            GameObject.FindGameObjectsWithTag("Enemy").Length - 1
        );

        Destroy(gameObject);
        Debug.Log("Enemy died");
    }
}