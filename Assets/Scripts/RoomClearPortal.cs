using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Spawns directly under the player when all enemies are dead in endless mode.
/// Sequence: appear under player -> brighten room -> suck player in -> fade to black -> load next room.
/// Total time ~3.5s. Call RoomClearPortal.Spawn() — fully self-contained.
/// </summary>
public class RoomClearPortal : MonoBehaviour
{
    [Header("Visual")]
    public float portalRadius = 1.5f;
    public float glowIntensity = 2f;
    public Color portalColor = new Color(0.3f, 0.6f, 1f);
    public float rotateSpeed = 300f;
    public float pulseSpeed = 4f;

    [Header("Timing (total ~2.75s)")]
    public float appearDuration = 0.35f;
    public float brightenDuration = 0.5f;
    public float suckDuration = 0.65f;
    public float fadeDuration = 0.45f;
    public float fadeHoldDuration = 0.4f;
    public float fadeInDuration = 0.4f;

    [Header("Room Brighten")]
    public float brightenIntensity = 0.6f;

    private Light2D portalLight;
    private Light2D roomLight;
    private Transform player;
    private SpriteRenderer[] rings;
    private Image fadeOverlay;
    private Canvas fadeCanvas;

    public static void Spawn()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        var portalObj = new GameObject("RoomClearPortal");
        var portal = portalObj.AddComponent<RoomClearPortal>();
        portal.player = playerObj.transform;

        // Spawn directly under the player
        portalObj.transform.position = playerObj.transform.position;
    }

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        BuildVisual();
        BuildFadeOverlay();
        StartCoroutine(PortalSequence());
    }

    void Update()
    {
        if (rings == null) return;

        foreach (var ring in rings)
        {
            if (ring != null)
                ring.transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
        }

        if (portalLight != null)
        {
            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * pulseSpeed);
            portalLight.intensity = glowIntensity * pulse;
        }
    }

    IEnumerator PortalSequence()
    {
        // Disable player movement immediately
        Rigidbody2D rb = null;
        PlayerMovement pm = null;
        if (player != null)
        {
            pm = player.GetComponent<PlayerMovement>();
            rb = player.GetComponent<Rigidbody2D>();
            if (pm != null) pm.enabled = false;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        // --- Phase 1: Portal appears under player (scale up from zero) ---
        float elapsed = 0f;
        transform.localScale = Vector3.zero;

        while (elapsed < appearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / appearDuration;
            float s = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        transform.localScale = Vector3.one;

        // --- Phase 2: Brighten the room ---
        roomLight = CreateRoomLight();
        elapsed = 0f;
        while (elapsed < brightenDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / brightenDuration;
            roomLight.intensity = Mathf.SmoothStep(0f, brightenIntensity, t);
            yield return null;
        }
        roomLight.intensity = brightenIntensity;

        // --- Phase 3: Suck the player in (shrink + spin into portal) ---
        elapsed = 0f;
        float playerStartAngle = 0f;

        while (elapsed < suckDuration)
        {
            if (player == null) break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / suckDuration);

            // Accelerating curve for dramatic effect
            float curve = t * t;

            // Shrink player
            float shrink = Mathf.Lerp(1f, 0f, curve);
            player.localScale = new Vector3(shrink, shrink, 1f);

            // Spin player (accelerating rotation)
            playerStartAngle += 360f * t * 2f * Time.deltaTime;
            player.rotation = Quaternion.Euler(0f, 0f, playerStartAngle);

            // Pull slightly toward portal center
            if (rb != null) rb.linearVelocity = Vector2.zero;
            player.position = Vector3.Lerp(player.position, transform.position, t * 0.3f);

            // Intensify portal glow
            if (portalLight != null)
                portalLight.intensity = glowIntensity * (1f + curve * 3f);

            yield return null;
        }

        // Snap player into portal
        if (player != null)
        {
            player.localScale = Vector3.zero;
            player.position = transform.position;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        // --- Phase 4: Fade to black ---
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            SetFade(Mathf.SmoothStep(0f, 1f, t));

            // Collapse portal during fade
            float portalScale = Mathf.Lerp(1f, 0f, t);
            transform.localScale = new Vector3(portalScale, portalScale, 1f);
            if (portalLight != null)
                portalLight.intensity = glowIntensity * portalScale * 4f;

            yield return null;
        }
        SetFade(1f);
        transform.localScale = Vector3.zero;

        // --- Phase 5: Hold black, load next room ---
        yield return new WaitForSeconds(fadeHoldDuration);

        // Restore player before loading
        if (player != null)
        {
            player.localScale = Vector3.one;
            player.rotation = Quaternion.identity;
            if (pm != null) pm.enabled = true;
        }

        if (roomLight != null)
            Destroy(roomLight.gameObject);

        // Wait for room build + collider regeneration before fading back in.
        // When returning from a boss scene, DungeonManager lives in the origin scene
        // (not here), so load that scene directly — DungeonManager.Start will restore state.
        if (DungeonManager.Instance != null)
        {
            var loadOp = DungeonManager.Instance.LoadNextRoom("none");
            if (loadOp != null) yield return loadOp;
        }
        else if (DungeonManager.IsReturningFromBoss && !string.IsNullOrEmpty(DungeonManager.OriginSceneName))
        {
            SceneManager.LoadScene(DungeonManager.OriginSceneName);
            yield break;  // portal & canvas destroyed by scene load; no fade-in needed
        }

        // --- Phase 6: Fade back in ---
        elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            SetFade(1f - Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        SetFade(0f);

        // Cleanup
        if (fadeCanvas != null)
            Destroy(fadeCanvas.gameObject);
        Destroy(gameObject);
    }

    void SetFade(float alpha)
    {
        if (fadeOverlay != null)
            fadeOverlay.color = new Color(0f, 0f, 0f, alpha);
    }

    void BuildFadeOverlay()
    {
        var canvasObj = new GameObject("PortalFadeCanvas");
        fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 200;

        var panelObj = new GameObject("FadePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        var rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeOverlay = panelObj.AddComponent<Image>();
        fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        fadeOverlay.raycastTarget = false;
    }

    Light2D CreateRoomLight()
    {
        // Destroy any leftover global lights from previous rooms to prevent stacking
        var lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.lightType == Light2D.LightType.Global && l.gameObject.name == "RoomClearLight")
            {
                Destroy(l.gameObject);
            }
        }

        var lightObj = new GameObject("RoomClearLight");
        var light = lightObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = new Color(1f, 0.97f, 0.9f);
        light.intensity = 0f;
        return light;
    }

    void BuildVisual()
    {
        portalLight = gameObject.AddComponent<Light2D>();
        portalLight.lightType = Light2D.LightType.Point;
        portalLight.color = portalColor;
        portalLight.intensity = glowIntensity;
        portalLight.pointLightOuterRadius = portalRadius * 2f;
        portalLight.pointLightInnerRadius = portalRadius * 0.3f;
        portalLight.pointLightOuterAngle = 360f;
        portalLight.pointLightInnerAngle = 360f;
        portalLight.shadowsEnabled = false;

        rings = new SpriteRenderer[3];
        float[] scales = { 1f, 0.7f, 0.4f };
        float[] alphas = { 0.4f, 0.6f, 0.9f };

        var sprite = CreateRingSprite();

        for (int i = 0; i < rings.Length; i++)
        {
            var ringObj = new GameObject("PortalRing" + i);
            ringObj.transform.SetParent(transform, false);
            ringObj.transform.localPosition = Vector3.zero;
            ringObj.transform.localScale = new Vector3(
                portalRadius * scales[i],
                portalRadius * scales[i] * 0.6f,
                1f);
            ringObj.transform.rotation = Quaternion.Euler(0f, 0f, i * 60f);

            var sr = ringObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.material = GetSpriteMaterial();
            sr.color = new Color(portalColor.r, portalColor.g, portalColor.b, alphas[i]);
            sr.sortingOrder = 10 + i;

            rings[i] = sr;
        }

        var centerObj = new GameObject("PortalCenter");
        centerObj.transform.SetParent(transform, false);
        centerObj.transform.localPosition = Vector3.zero;
        centerObj.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        var centerSr = centerObj.AddComponent<SpriteRenderer>();
        centerSr.sprite = sprite;
        centerSr.material = GetSpriteMaterial();
        centerSr.color = new Color(0.8f, 0.9f, 1f, 0.95f);
        centerSr.sortingOrder = 13;
    }

    static Sprite CreateRingSprite()
    {
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float outerR = center - 1f;
        float innerR = outerR * 0.65f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float a = 0f;

                if (dist <= outerR && dist >= innerR)
                {
                    float edgeOuter = Mathf.Clamp01(1f - (dist - outerR + 1f));
                    float edgeInner = Mathf.Clamp01((dist - innerR) / 2f);
                    a = edgeOuter * edgeInner;
                }

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Material cachedSpriteMat;
    static Material GetSpriteMaterial()
    {
        if (cachedSpriteMat == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
                cachedSpriteMat = new Material(shader);
        }
        return cachedSpriteMat;
    }
}
