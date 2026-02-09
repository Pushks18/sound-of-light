using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ImpactLightStatic : MonoBehaviour
{
    public float duration = 1.0f;

    void Start()
    {
        // 只负责销毁，不要去动 radius
        Destroy(gameObject, duration);
    }
}