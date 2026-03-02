using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class PlayerShooting : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public Light2D muzzleFlashLight;
    public float flashDuration = 0.05f;

    public float energyCost = 1f;

    private PlayerMovement playerMovement;
    private LightEnergy lightEnergy;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        lightEnergy = GetComponent<LightEnergy>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null || playerMovement == null)
        {
            Debug.Log($"[Shoot] Blocked: bulletPrefab={bulletPrefab != null}, firePoint={firePoint != null}, playerMovement={playerMovement != null}");
            return;
        }

        // Check both resources before spending either
        if (PlayerAmmo.Instance != null && PlayerAmmo.Instance.Bullets <= 0)
        {
            Debug.Log("[Shoot] Out of ammo!");
            return;
        }

        if (lightEnergy != null && !lightEnergy.CanSpend(energyCost))
        {
            Debug.Log($"[Shoot] Not enough energy. Current: {lightEnergy.CurrentEnergy}, Cost: {energyCost}");
            return;
        }

        // Both checks passed — now spend
        PlayerAmmo.Instance?.TrySpendBullet();
        lightEnergy?.TrySpend(energyCost);

        Vector2 direction = playerMovement.AimDirection;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        var bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(0, 0, angle));
        Debug.Log($"[Shoot] Bullet fired at {firePoint.position}, direction={direction}, angle={angle}");

        if (muzzleFlashLight != null)
        {
            StartCoroutine(FlashRoutine());
        }
    }

    IEnumerator FlashRoutine()
    {
        muzzleFlashLight.intensity = 0.4f;
        yield return new WaitForSeconds(flashDuration);
        muzzleFlashLight.intensity = 0f;
    }
}
