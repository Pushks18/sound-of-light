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
            return;

        if (PlayerAmmo.Instance == null)
            return;

        if (!PlayerAmmo.Instance.TrySpendBullet())
            return;

        if (lightEnergy != null && !lightEnergy.TrySpend(energyCost))
            return;

        Vector2 direction = playerMovement.AimDirection;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(0, 0, angle));

        if (muzzleFlashLight != null)
            StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        muzzleFlashLight.intensity = 0.4f;
        yield return new WaitForSeconds(flashDuration);
        muzzleFlashLight.intensity = 0f;
    }
}
