using UnityEngine;

[DefaultExecutionOrder(100)] // runs after CameraFollow (default order 0)
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private float shakeDuration;
    private float shakeMagnitude;
    private float shakeElapsed;
    private bool  isShaking;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Shake(float duration, float magnitude)
    {
        shakeDuration  = duration;
        shakeMagnitude = magnitude;
        shakeElapsed   = 0f;
        isShaking      = true;
    }

    void LateUpdate()
    {
        if (!isShaking) return;

        shakeElapsed += Time.unscaledDeltaTime;
        if (shakeElapsed >= shakeDuration)
        {
            isShaking = false;
            return;
        }

        // CameraFollow has already positioned the camera this frame — add offset on top
        float x = Random.Range(-1f, 1f) * shakeMagnitude;
        float y = Random.Range(-1f, 1f) * shakeMagnitude;
        transform.position += new Vector3(x, y, 0f);
    }
}
