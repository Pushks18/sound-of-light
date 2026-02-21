using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ExplosionLight : MonoBehaviour
{
    private Light2D light2D;
    public float fadeSpeed = 4f;

    void Start()
    {
        light2D = GetComponent<Light2D>();
    }

    void Update()
    {
        light2D.intensity -= fadeSpeed * Time.deltaTime;

        if (light2D.intensity <= 0)
        {
            Destroy(gameObject);
        }
    }
}