using UnityEngine;
using TMPro;

/// <summary>
/// Floating reward text (e.g. "+1 ♥", "+Ammo") that appears above the player
/// at the start of a room-clear portal sequence.
/// No prefab required — call RewardPopup.Spawn() from anywhere.
/// </summary>
public class RewardPopup : MonoBehaviour
{
    private const float FloatSpeed = 0.85f;
    private const float Lifetime   = 2.4f;
    private const float FadeStart  = 1.4f;

    private TextMeshPro tmp;
    private float elapsed;

    public static void Spawn(string text, Color color, Vector3 worldPos)
    {
        var go  = new GameObject("RewardPopup");
        go.transform.position = worldPos;

        var rp  = go.AddComponent<RewardPopup>();
        var tmp = go.AddComponent<TextMeshPro>();

        tmp.text         = text;
        tmp.fontSize     = 11f;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.color        = color;
        tmp.outlineWidth = 0.3f;
        tmp.outlineColor = new Color32(0, 0, 0, 220);
        tmp.sortingOrder = 100;

        rp.tmp = tmp;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += Vector3.up * FloatSpeed * Time.deltaTime;

        if (tmp != null)
        {
            // Quick pop-in scale, then settle to 1
            float scale = elapsed < 0.12f ? Mathf.Lerp(1.5f, 1f, elapsed / 0.12f) : 1f;
            transform.localScale = Vector3.one * scale;

            // Hold full opacity, then fade out
            float fade = elapsed < FadeStart
                ? 1f
                : Mathf.Lerp(1f, 0f, (elapsed - FadeStart) / (Lifetime - FadeStart));
            var c = tmp.color;
            c.a = fade;
            tmp.color = c;
        }

        if (elapsed >= Lifetime)
            Destroy(gameObject);
    }
}
