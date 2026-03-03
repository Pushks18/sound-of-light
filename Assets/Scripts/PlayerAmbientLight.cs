using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerAmbientLight : MonoBehaviour
{
    public float ambientRadius = 1.5f;
    public float ambientIntensity = 0.4f;
    public Color ambientColor = new Color(0.7f, 0.8f, 1f);

    void Start()
    {
        var ambientObj = new GameObject("AmbientLight");
        ambientObj.transform.SetParent(transform);
        ambientObj.transform.localPosition = Vector3.zero;
        // No "LightSource" tag -- doesn't activate enemies

        var light = ambientObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = ambientColor;
        light.intensity = ambientIntensity;
        light.pointLightOuterRadius = ambientRadius;
        light.pointLightInnerRadius = ambientRadius * 0.3f;
        light.pointLightOuterAngle = 360f;
        light.pointLightInnerAngle = 360f;
        light.shadowsEnabled = false;
        light.falloffIntensity = 0.7f;
    }
}
