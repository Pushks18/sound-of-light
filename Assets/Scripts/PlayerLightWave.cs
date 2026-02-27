using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerLightWave : MonoBehaviour
{
    [Header("Light Wave Settings")]
    public float waveRadius = 12f;
    public float waveIntensity = 2.5f;
    public float waveDuration = 5f;
    public float energyCost = 10f;
    public float cooldown = 20f;
    public Color waveColor = new Color(1f, 0.95f, 0.8f);

    private LightEnergy lightEnergy;
    private float cooldownTimer;

    void Start()
    {
        lightEnergy = GetComponent<LightEnergy>();
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        GameUIManager.Instance?.UpdateFlash(cooldownTimer > 0f ? cooldownTimer : 0f);

        if (Input.GetKeyDown(KeyCode.L) && cooldownTimer <= 0f)
        {
            // Check flash count
            if (PlayerAmmo.Instance != null && !PlayerAmmo.Instance.TrySpendFlash())
            {
                Debug.Log("[Flash] No flashes remaining!");
            }
            else if (lightEnergy != null && lightEnergy.TrySpend(energyCost))
            {
                EmitLightWave();
                cooldownTimer = cooldown;
                StatusHUD.Instance?.StartFlashCooldown(cooldown);
            }
        }
    }

    void EmitLightWave()
    {
        var waveObj = new GameObject("LightWave");
        waveObj.transform.position = transform.position;
        waveObj.tag = "LightSource";

        var light = waveObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = waveColor;
        light.intensity = waveIntensity;
        light.pointLightOuterRadius = waveRadius;
        light.pointLightInnerRadius = waveRadius * 0.3f;
        light.pointLightOuterAngle = 360f;
        light.pointLightInnerAngle = 360f;
        light.shadowsEnabled = true;
        light.falloffIntensity = 0.5f;

        var collider = waveObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = waveRadius;

        // Attach fade behavior
        var fader = waveObj.AddComponent<LightWaveFader>();
        fader.duration = waveDuration;
        fader.startIntensity = waveIntensity;
    }
}

public class LightWaveFader : MonoBehaviour
{
    [HideInInspector] public float duration = 1f;
    [HideInInspector] public float startIntensity = 2.5f;

    private Light2D light2D;
    private float elapsed;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;

        if (light2D != null)
            light2D.intensity = Mathf.Lerp(startIntensity, 0f, t);

        if (elapsed >= duration)
        {
            // Disable collider before Destroy so OnTriggerExit2D fires on enemies
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            Destroy(gameObject);
        }
    }
}
