using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal; // 必须有这一行才能控制灯光

public class PlayerShooting : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public Light2D muzzleFlashLight; // 拖入你角色身上的 MuzzleLight
    public float flashDuration = 0.1f; // 闪烁时间

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (bulletPrefab != null && firePoint != null)
        {
            // 1. 计算从枪口到鼠标的方向
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0; // 确保在2D平面上
            Vector2 direction = (mousePos - firePoint.position).normalized;

            // 2. 计算子弹该旋转的角度
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

            // 3. 生成子弹并设置它的角度
            Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(0, 0, angle));

            // 4. 让闪光亮起
            if (muzzleFlashLight != null)
            {
                StartCoroutine(FlashRoutine());
            }
        }
    }

    IEnumerator FlashRoutine()
    {
        muzzleFlashLight.intensity = 2f; // 变亮
        yield return new WaitForSeconds(flashDuration);
        muzzleFlashLight.intensity = 0f; // 熄灭
    }
}