using UnityEngine;

public class EnemyShooting : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;

    public float shootRange = 3f;
    public float fireCooldown = 0.5f;

    private Transform player;
    private float fireTimer;
    private EnemyAI enemyAI;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        enemyAI = GetComponent<EnemyAI>();
        fireTimer = fireCooldown;
    }

    void Update()
    {
        if (player == null || enemyAI == null)
            return;

        if (!enemyAI.IsActivated || enemyAI.IsStunned())
            return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= shootRange)
        {
            fireTimer -= Time.deltaTime;

            if (fireTimer <= 0f)
            {
                Shoot();
                fireTimer = fireCooldown;
            }
        }
        else
        {
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
