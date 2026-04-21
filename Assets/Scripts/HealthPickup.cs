using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

/// <summary>
/// Green health collectible that looks like a trap but heals the player on contact.
/// Spawned in endless mode when the player is missing HP.
/// Registers with a static list so the minimap can display it.
/// </summary>
public class HealthPickup : MonoBehaviour
{
    // Static registry for minimap access (no per-frame FindObjectsByType)
    private static readonly List<HealthPickup> allPickups = new List<HealthPickup>();
    public static IReadOnlyList<HealthPickup> AllPickups => allPickups;

    [Header("Healing")]
    public int healAmount = 1;

    [Header("Glow")]
    public Color glowColor = new Color(0.2f, 1f, 0.3f, 1f);
    public float glowRadius = 1.5f;
    public float glowIntensity = 0.8f;
    public float pulseSpeed = 2f;

    [Header("Burst on Pickup")]
    public float burstRadius = 3f;
    public float burstIntensity = 1.5f;
    public float burstDuration = 0.5f;

    private Light2D glowLight;
    private bool collected;

    void Start()
    {
        allPickups.Add(this);

        // Ensure trigger collider
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        // Kinematic rigidbody for trigger detection (same pattern as Trap)
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Green pulsing glow so the player can spot it in the dark
        var lightObj = new GameObject("PickupGlow");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = Vector3.zero;

        glowLight = lightObj.AddComponent<Light2D>();
        glowLight.lightType = Light2D.LightType.Point;
        glowLight.color = glowColor;
        glowLight.intensity = glowIntensity;
        glowLight.pointLightOuterRadius = glowRadius;
        glowLight.pointLightInnerRadius = glowRadius * 0.3f;
        glowLight.pointLightOuterAngle = 360f;
        glowLight.pointLightInnerAngle = 360f;
        glowLight.shadowsEnabled = false;
    }

    void Update()
    {
        if (glowLight != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
            glowLight.intensity = Mathf.Lerp(glowIntensity * 0.3f, glowIntensity, pulse);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        var hp = other.GetComponent<PlayerHealth>();
        if (hp == null) return;

        // Don't pick up if already at full health
        if (hp.currentHealth >= hp.maxHealth) return;

        collected = true;

        hp.currentHealth = Mathf.Min(hp.currentHealth + healAmount, hp.maxHealth);
        StatusHUD.Instance?.UpdateHP(hp.currentHealth, hp.maxHealth);

        SpawnBurst();
        Destroy(gameObject);
    }

    void SpawnBurst()
    {
        var burstObj = new GameObject("HealthBurst");
        burstObj.transform.position = transform.position;
        burstObj.tag = "LightSource";

        var light = burstObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = glowColor;
        light.intensity = burstIntensity;
        light.pointLightOuterRadius = burstRadius;
        light.pointLightInnerRadius = burstRadius * 0.3f;
        light.pointLightOuterAngle = 360f;
        light.pointLightInnerAngle = 360f;
        light.shadowsEnabled = false;

        var col = burstObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = burstRadius;

        // Reuse the same timed destroy component that Trap uses
        var destroyer = burstObj.AddComponent<TimedLightBurstDestroy>();
        destroyer.delay = burstDuration;
    }

    void OnDestroy()
    {
        allPickups.Remove(this);
    }
}
