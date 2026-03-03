using UnityEngine;

public class WebGLOptimizer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Disable Unity vsync — in WebGL the browser's requestAnimationFrame
        // already governs frame timing, so vSyncCount > 0 only adds stutter.
        QualitySettings.vSyncCount = 0;

        // Let the browser drive the frame rate (typically 60 Hz, or higher
        // on high-refresh displays).  -1 = "as fast as possible", which in
        // WebGL means "match requestAnimationFrame".
        Application.targetFrameRate = -1;
#endif
    }
}
