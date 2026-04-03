using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Gives room light sources a slow breathing pulse — same feel as the idle auto-flash.
/// Attached automatically by TutorialLayoutGenerator to each spawned light.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class TutorialLightPulse : MonoBehaviour
{
    [HideInInspector] public float minIntensity = 0.6f;
    [HideInInspector] public float maxIntensity = 1.5f;
    [HideInInspector] public float speed        = 0.8f;   // full cycles per second (slow breath)
    [HideInInspector] public float phase        = 0f;     // set randomly per light so they don't sync

    private Light2D light2D;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
    }

    void Update()
    {
        if (light2D == null) return;
        float t = (Mathf.Sin(Time.time * speed * Mathf.PI * 2f + phase) + 1f) * 0.5f;
        light2D.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
    }
}
