using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

public class Trap : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 1;
    public float armDelay = 2f;
    public float cooldownAfterHit = 2f;
    public bool affectsPlayer = true;
    public bool affectsEnemies = true;

    [Tooltip("If true, the trap starts Armed immediately without needing a light source to reveal it.")]
    public bool startArmed = false;

    [Header("Light Burst on Hit")]
    public bool emitLightOnHit = true;
    public float burstLightRadius = 3f;
    public float burstLightIntensity = 1.5f;
    public float burstLightDuration = 0.5f;
    public Color burstLightColor = new Color(1f, 0.4f, 0.2f);

    [Header("Reveal Light (stays lit once identified)")]
    public float revealLightRadius = 1.8f;
    public float revealLightIntensity = 0.8f;
    public Color revealSafeColor = new Color(1f, 0.95f, 0.4f);
    public Color revealArmedColor = new Color(1f, 0.15f, 0.05f);
    public float pulseSpeed = 3f;

    private enum State { Dormant, Arming, Armed }
    private State state = State.Dormant;

    private Light2D revealLight;
    private float armedBaseIntensity;
    private Dictionary<int, float> cooldownTimers = new Dictionary<int, float>();

    void Start()
    {
        // Kinematic Rigidbody2D lets the trap detect light source triggers
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Auto-arm if flagged (e.g. Tutorial scene traps that must always be active)
        if (startArmed)
        {
            state = State.Armed;
        }
    }

    void Update()
    {
        // Tick down per-entity cooldowns
        var expired = new List<int>();
        foreach (var kvp in cooldownTimers)
        {
            if (Time.time >= kvp.Value)
                expired.Add(kvp.Key);
        }
        foreach (int key in expired)
            cooldownTimers.Remove(key);

        // Pulse the light when armed so the player can clearly see the danger
        if (state == State.Armed && revealLight != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
            revealLight.intensity = Mathf.Lerp(armedBaseIntensity * 0.4f, armedBaseIntensity, pulse);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Light reveals and starts arming the trap — but only if the
        // light actually has intensity (filters out the always-present
        // MuzzleLight whose intensity is 0 when not firing).
        if (other.CompareTag("LightSource"))
        {
            if (state == State.Dormant)
            {
                var light = other.GetComponent<Light2D>();
                if (light != null && light.intensity > 0f)
                    StartCoroutine(RevealAndArm());
            }
            return;
        }

        // Only deal damage when armed
        if (state != State.Armed)
            return;

        int id = other.gameObject.GetInstanceID();
        if (cooldownTimers.ContainsKey(id))
            return;

        if (affectsPlayer && other.CompareTag("Player"))
        {
            cooldownTimers[id] = Time.time + cooldownAfterHit;
            StartCoroutine(ApplyDamagePlayerDelayed(other));
        }
        else if (affectsEnemies && other.CompareTag("Enemy"))
        {
            cooldownTimers[id] = Time.time + cooldownAfterHit;
            var health = other.GetComponent<EnemyHealth>();
            if (health != null)
                StartCoroutine(ApplyDamageEnemy(health));
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (state != State.Armed)
            return;

        // Re-trigger damage if the entity is still standing on the trap
        // and their cooldown has expired.
        int id = other.gameObject.GetInstanceID();
        if (cooldownTimers.ContainsKey(id))
            return;

        if (affectsPlayer && other.CompareTag("Player"))
        {
            cooldownTimers[id] = Time.time + cooldownAfterHit;
            StartCoroutine(ApplyDamagePlayerDelayed(other));
        }
        else if (affectsEnemies && other.CompareTag("Enemy"))
        {
            cooldownTimers[id] = Time.time + cooldownAfterHit;
            var health = other.GetComponent<EnemyHealth>();
            if (health != null)
                StartCoroutine(ApplyDamageEnemy(health));
        }
    }

    IEnumerator RevealAndArm()
    {
        state = State.Arming;

        // Create the permanent reveal light
        var lightObj = new GameObject("TrapRevealLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = Vector3.zero;

        revealLight = lightObj.AddComponent<Light2D>();
        revealLight.lightType = Light2D.LightType.Point;
        revealLight.color = revealSafeColor;
        revealLight.intensity = 0f;
        revealLight.pointLightOuterRadius = revealLightRadius;
        revealLight.pointLightInnerRadius = revealLightRadius * 0.3f;
        revealLight.pointLightOuterAngle = 360f;
        revealLight.pointLightInnerAngle = 360f;
        revealLight.shadowsEnabled = false;

        // Arming phase: yellow light fades in over the arm delay
        float elapsed = 0f;
        while (elapsed < armDelay)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / armDelay;
            if (revealLight != null)
                revealLight.intensity = Mathf.Lerp(0f, revealLightIntensity, t);
            yield return null;
        }

        // Armed: snap to red, start pulsing (handled in Update)
        state = State.Armed;
        if (revealLight != null)
        {
            armedBaseIntensity = revealLightIntensity * 1.8f;
            revealLight.color = revealArmedColor;
            revealLight.intensity = armedBaseIntensity;
            revealLight.pointLightOuterRadius = revealLightRadius * 1.3f;
        }
    }

    IEnumerator ApplyDamagePlayerDelayed(Collider2D playerCollider)
    {
        // Small delay so the player can dash through
        yield return new WaitForSeconds(0.15f);

        // Player dodges the trap if still dashing
        var movement = playerCollider != null ? playerCollider.GetComponent<PlayerMovement>() : null;
        if (movement != null && movement.IsDashing)
            yield break;

        var player = playerCollider != null ? playerCollider.GetComponent<PlayerHealth>() : null;
        if (player != null)
            player.TakeDamage(damage);

        if (emitLightOnHit)
            SpawnLightBurst();
    }

    IEnumerator ApplyDamageEnemy(EnemyHealth health)
    {
        yield return new WaitForSeconds(0.15f);
        if (health != null)
            health.TakeDamage(damage);

        if (emitLightOnHit)
            SpawnLightBurst();
    }

    void SpawnLightBurst()
    {
        var lightObj = new GameObject("TrapLightBurst");
        lightObj.transform.position = transform.position;
        lightObj.tag = "LightSource";

        var light = lightObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = burstLightColor;
        light.intensity = burstLightIntensity;
        light.pointLightOuterRadius = burstLightRadius;
        light.pointLightInnerRadius = burstLightRadius * 0.3f;
        light.pointLightOuterAngle = 360f;
        light.pointLightInnerAngle = 360f;
        light.shadowsEnabled = false;

        var col = lightObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = burstLightRadius;

        // Self-destruct on the light burst itself so it cleans up
        // even if this trap is destroyed mid-burst
        var destroyer = lightObj.AddComponent<TimedLightBurstDestroy>();
        destroyer.delay = burstLightDuration;
    }
}

public class TimedLightBurstDestroy : MonoBehaviour
{
    [HideInInspector] public float delay;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(delay);
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        Destroy(gameObject);
    }
}
