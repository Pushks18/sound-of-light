using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class LightFader : MonoBehaviour
{
    private Light2D lightSource;
    private CircleCollider2D lightTrigger;

    [Header("时间设置")]
    public float keepTime = 1.5f;      // 全亮保持的时间
    public float fadeDuration = 0.5f;  // 逐渐消失的时间

    private float maxIntensity;

    void Awake()
    {
        lightSource = GetComponent<Light2D>();
        lightTrigger = GetComponent<CircleCollider2D>();

        if (lightSource != null)
        {
            maxIntensity = lightSource.intensity;
            StartCoroutine(FadeRoutine());
        }
        else
        {
            Destroy(gameObject, keepTime + fadeDuration);
        }
    }

    IEnumerator FadeRoutine()
    {
        // 1. 保持全亮阶段
        yield return new WaitForSeconds(keepTime);

        // 2. 渐弱阶段
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float lerpVal = elapsed / fadeDuration;

            // 亮度线性变暗
            lightSource.intensity = Mathf.Lerp(maxIntensity, 0f, lerpVal);

            yield return null;
        }

        // 3. 彻底熄灭后销毁
        lightSource.intensity = 0f;

        // 关键：关闭感应，防止灯都要没了还能吸引怪
        if (lightTrigger != null) lightTrigger.enabled = false;

        Destroy(gameObject);
    }
}