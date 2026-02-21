using UnityEngine;

public class EnemyHealth : MonoBehaviour

{
    public int maxHealth = 2;
    public GameObject hitLightPrefab;
    public GameObject hitGlowPrefab;
    public ParticleSystem glowParticles;

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
        Destroy(gameObject);
        Debug.Log("Enemy died");
    }
}