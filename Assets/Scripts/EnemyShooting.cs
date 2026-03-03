using UnityEngine;

public class EnemyShooting : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;

    public float shootRange = 10f;
    public float fireCooldown = 1.2f;

    private Transform player;
    private float fireTimer;
    private EnemyAI enemyAI;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        enemyAI = GetComponent<EnemyAI>();
        fireTimer = 0f; // ready to fire immediately
    }

    void Update()
    {
        if (player == null || enemyAI == null)
            return;

        if (!enemyAI.IsActivated || enemyAI.IsStunned())
            return;

        // Always count down, so enemies fire immediately when player enters range
        fireTimer -= Time.deltaTime;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= shootRange && fireTimer <= 0f)
        {
            Shoot();
            fireTimer = fireCooldown;
        }
    }

    void Shoot()
    {
        Vector2 dir = (player.position - firePoint.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(0, 0, angle));
    }
}
