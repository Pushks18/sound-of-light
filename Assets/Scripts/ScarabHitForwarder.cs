using UnityEngine;

/// <summary>
/// Attach to Scarab child colliders (Head, WingL, WingR) so slash/bullet hits
/// on those parts are forwarded to the parent ScarabAI for processing.
/// Requires a Collider2D (isTrigger = true) on the same GameObject.
/// </summary>
public class ScarabHitForwarder : MonoBehaviour
{
    [SerializeField] bool isHead = false;  // tick on Head child; leave unchecked on WingL/WingR

    ScarabAI scarab;

    void Awake()
    {
        scarab = GetComponentInParent<ScarabAI>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (scarab == null) return;

        bool isSlash  = other.CompareTag("LightSource") && other.GetComponent<SlashBulletDeflector>() != null;
        bool isBullet = other.CompareTag("Bullet");

        if (isSlash || isBullet)
            scarab.OnArmorPartHit(other, isBullet);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (scarab == null) return;
        if (!other.CompareTag("Player")) return;
        scarab.OnArmorPlayerContact(transform.position, isHead);
    }
}
