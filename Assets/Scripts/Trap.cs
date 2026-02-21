using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class Trap : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 1;
    public float cooldown = 2f;
    public float triggerDelay = 0.2f;
    public bool affectsPlayer = true;
    public bool affectsEnemies = true;

    [Header("Light Burst on Activation")]
    public bool emitLightOnActivation = false;
    public float lightRadius = 3f;
    public float lightIntensity = 1.5f;
    public float lightDuration = 0.5f;
    public Color lightColor = new Color(1f, 0.4f, 0.2f);

    private Dictionary<int, float> cooldownTimers = new Dictionary<int, float>();

    void Update()
    {
        // Tick down cooldowns
        var expired = new List<int>();
        foreach (var kvp in cooldownTimers)
        {
            if (Time.time >= kvp.Value)
                expired.Add(kvp.Key);
        }
        foreach (int key in expired)
            cooldownTimers.Remove(key);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        int id = other.gameObject.GetInstanceID();
        if (cooldownTimers.ContainsKey(id))
            return;

        if (affectsPlayer && other.CompareTag("Player"))
        {
            cooldownTimers[id] = Time.time + cooldown;
            Invoke(nameof(ApplyDamagePlayer), triggerDelay);
            if (emitLightOnActivation)
                SpawnLightBurst();
        }
        else if (affectsEnemies && other.CompareTag("Enemy"))
        {
            cooldownTimers[id] = Time.time + cooldown;
            // Store reference for delayed damage
            var health = other.GetComponent<EnemyHealth>();
            if (health != null)
                StartCoroutine(ApplyDamageEnemy(health));
            if (emitLightOnActivation)
                SpawnLightBurst();
        }
    }

    void ApplyDamagePlayer()
    {
        var player = FindAnyObjectByType<PlayerHealth>();
        if (player != null)
            player.TakeDamage(damage);
    }

    System.Collections.IEnumerator ApplyDamageEnemy(EnemyHealth health)
    {
        yield return new WaitForSeconds(triggerDelay);
        if (health != null)
            health.TakeDamage(damage);
    }

    void SpawnLightBurst()
    {
        var lightObj = new GameObject("TrapLightBurst");
        lightObj.transform.position = transform.position;
        lightObj.tag = "LightSource";

        var light = lightObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = lightColor;
        light.intensity = lightIntensity;
        light.pointLightOuterRadius = lightRadius;
        light.pointLightInnerRadius = lightRadius * 0.3f;
        light.pointLightOuterAngle = 360f;
        light.pointLightInnerAngle = 360f;
        light.shadowsEnabled = false;

        var collider = lightObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = lightRadius;

        Destroy(lightObj, lightDuration);
    }
}
