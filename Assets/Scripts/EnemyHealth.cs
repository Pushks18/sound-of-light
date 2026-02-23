using UnityEngine;

public class EnemyHealth : MonoBehaviour

{
    public int maxHealth = 2;
    public GameObject hitLightPrefab;
    public GameObject hitGlowPrefab;
    public ParticleSystem glowParticles;

    [HideInInspector] public int currentHealth;

    [SerializeField] private GameUIManager gameUIManager;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        GameObject gameUIManagerGameObject = GameObject.Find("HealthUI");
        gameUIManager = gameUIManagerGameObject.GetComponent<GameUIManager>();
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
        gameUIManager.UpdateEnemyCount(GameObject.FindGameObjectsWithTag("Enemy").Length - 1);
        Destroy(gameObject);
        Debug.Log("Enemy died");
    }
}