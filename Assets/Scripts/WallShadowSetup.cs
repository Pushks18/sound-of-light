using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Reflection;

public class WallShadowSetup : MonoBehaviour
{
    void Awake()
    {
        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (obj.name.StartsWith("Wall") && obj.GetComponent<Collider2D>() != null)
            {
                if (obj.GetComponent<ShadowCaster2D>() == null)
                {
                    var caster = obj.AddComponent<ShadowCaster2D>();
                    caster.selfShadows = true;
                    SetShadowPathFromCollider(caster, obj.GetComponent<Collider2D>());
                }
            }
        }
    }

    void SetShadowPathFromCollider(ShadowCaster2D caster, Collider2D collider)
    {
        Vector3[] path = null;

        if (collider is BoxCollider2D box)
        {
            Vector2 half = box.size * 0.5f;
            Vector2 o = box.offset;
            path = new Vector3[]
            {
                new Vector3(o.x - half.x, o.y - half.y, 0f),
                new Vector3(o.x + half.x, o.y - half.y, 0f),
                new Vector3(o.x + half.x, o.y + half.y, 0f),
                new Vector3(o.x - half.x, o.y + half.y, 0f)
            };
        }
        else if (collider is PolygonCollider2D poly)
        {
            Vector2[] pts = poly.points;
            path = new Vector3[pts.Length];
            for (int i = 0; i < pts.Length; i++)
                path[i] = new Vector3(pts[i].x, pts[i].y, 0f);
        }

        if (path == null) return;

        var shapeField = typeof(ShadowCaster2D).GetField("m_ShapePath",
            BindingFlags.NonPublic | BindingFlags.Instance);
        shapeField?.SetValue(caster, path);

        var hashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash",
            BindingFlags.NonPublic | BindingFlags.Instance);
        hashField?.SetValue(caster, Random.Range(1, int.MaxValue));
    }
}
