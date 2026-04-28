using UnityEngine;
using TMPro;

/// <summary>
/// Spawns a floating "-N" damage number above a world-space position,
/// floats upward and fades out, then destroys itself.
/// No prefab required — call DamageNumber.Spawn() from anywhere.
/// </summary>
public class DamageNumber : MonoBehaviour
{
    private const float FloatSpeed = 1.5f;
    private const float Lifetime   = 1.5f;   // how long it stays visible

    private TextMeshPro tmp;
    private float elapsed;

    // ── Public API ───────────────────────────────────────────────────────────
    public static void Spawn(int amount, Vector3 worldPos, bool isPlayer = false)
    {
        var go = new GameObject("DamageNumber");

        // Slight random scatter so stacked hits stay readable
        float xOffset = Random.Range(-0.25f, 0.25f);
        float yOffset = Random.Range(0.4f, 0.7f);
        go.transform.position = worldPos + new Vector3(xOffset, yOffset, 0f);

        var dn  = go.AddComponent<DamageNumber>();
        var tmp = go.AddComponent<TextMeshPro>();

        tmp.text      = "-" + amount;
        tmp.fontSize  = 14f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = new Color(1f, 0.22f, 0.1f, 1f); // hot red-orange
        if (isPlayer)
            tmp.color = new Color(1f, 1f, 1f, 1f); // white for player

        // Outline so it pops on any background
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(0, 0, 0, 200);

        tmp.sortingOrder = 100;

        dn.tmp = tmp;
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────
    void Update()
    {
        elapsed += Time.deltaTime;

        // Float upward
        transform.position += Vector3.up * FloatSpeed * Time.deltaTime;

        float t = elapsed / Lifetime;

        if (tmp != null)
        {
            // Stay fully opaque for first 40% of lifetime, then fade out
            float fade = t < 0.4f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.4f) / 0.6f);
            var c = tmp.color;
            c.a = fade;
            tmp.color = c;

            // Slight scale-up pop at start, then shrink back
            float scale = t < 0.15f
                ? Mathf.Lerp(1.3f, 1f, t / 0.15f)   // quick pop-in
                : Mathf.Lerp(1f, 0.7f, (t - 0.15f) / 0.85f);
            transform.localScale = Vector3.one * scale;
        }

        if (elapsed >= Lifetime)
            Destroy(gameObject);
    }
}
