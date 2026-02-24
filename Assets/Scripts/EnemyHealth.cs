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
            Debug.Log("Enemy took damage");

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
    if (GameManager.Instance == null)
{
    Debug.LogError("GameManager.Instance is NULL");
}
else
{
    Debug.Log("GameManager.Instance found");
}

    OnEnemyKilled?.Invoke();

    GameManager.Instance?.EnemyKilled();

    Destroy(gameObject);

    Debug.Log("Enemy died");
}
}