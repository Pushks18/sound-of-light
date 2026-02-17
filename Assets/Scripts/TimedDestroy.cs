using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EchoPulse : MonoBehaviour
{
    private Light2D light2D;
    public float expandSpeed = 15f;
    public float fadeSpeed = 2f;
    private float currentIntensity;
    private float currentRadius;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        currentIntensity = light2D.intensity;
        currentRadius = 0f;
        light2D.pointLightOuterRadius = 0f;
        light2D.pointLightInnerRadius = 0f;
    }

    void Update()
    {
        // Expand the light radius directly
        currentRadius += expandSpeed * Time.deltaTime;
        light2D.pointLightOuterRadius = currentRadius;
        light2D.pointLightInnerRadius = currentRadius * 0.5f;

        // Fade intensity
        currentIntensity -= fadeSpeed * Time.deltaTime;
        light2D.intensity = currentIntensity;

        if (currentIntensity <= 0)
        {
            Destroy(gameObject);
        }
    }
}