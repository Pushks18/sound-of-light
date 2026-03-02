using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class EnemyHitGlow : MonoBehaviour
{
    public float glowIntensity = 1.5f;
    public float glowDuration = 1f;

    private Light2D light2D;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        if (light2D != null)
            light2D.intensity = 0f;
        Destroy(gameObject, glowDuration + 0.1f);
    }

    public void TriggerGlow()
    {
        StopAllCoroutines();
        StartCoroutine(GlowRoutine());
    }

    IEnumerator GlowRoutine()
    {
        light2D.intensity = glowIntensity;

        float timer = 0f;

        while (timer < glowDuration)
        {
            timer += Time.deltaTime;
            light2D.intensity = Mathf.Lerp(glowIntensity, 0f, timer / glowDuration);
            yield return null;
        }

        light2D.intensity = 0f;
    }
}