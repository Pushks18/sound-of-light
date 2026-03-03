using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ImpactLightStatic : MonoBehaviour
{
    public float duration = 1.0f;

    void Awake()
    {
        // Disable shadows so the light doesn't create artifacts at wall seams
        var light = GetComponent<Light2D>();
        if (light != null)
            light.shadowsEnabled = false;
    }

    void Start()
    {
        Destroy(gameObject, duration);
    }
}