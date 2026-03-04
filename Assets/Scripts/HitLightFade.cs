using UnityEngine;
using UnityEngine.Rendering.Universal;

public class HitLightFade : MonoBehaviour
{
    public float lifetime = 1f;
    private float timer = 0f;
    private Light2D light2D;

    void Start()
    {
        light2D = GetComponent<Light2D>();
    }

    void Update()
    {
        timer += Time.deltaTime;

        float t = timer / lifetime;
        if (light2D != null)
            light2D.intensity = Mathf.Lerp(1.5f, 0f, t);

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}