using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EchoPulse : MonoBehaviour
{
    private Light2D light2D;
    public float expandSpeed = 15f;    // 扩散速度
    public float fadeSpeed = 2f;      // 消失速度
    private float currentIntensity;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        currentIntensity = light2D.intensity;
        // 初始大小设小一点
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        // 1. 让圆环向外扩张
        transform.localScale += Vector3.one * expandSpeed * Time.deltaTime;

        // 2. 让亮度逐渐变暗
        currentIntensity -= fadeSpeed * Time.deltaTime;
        light2D.intensity = currentIntensity;

        // 3. 当完全变黑时，销毁自己
        if (currentIntensity <= 0)
        {
            Destroy(gameObject);
        }
    }
}