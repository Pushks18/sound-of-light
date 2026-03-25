using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Circular radar minimap that shows enemy positions relative to the player.
/// Attach to the Player GameObject — builds its own UI, no prefab needed.
/// </summary>
public class MinimapRadar : MonoBehaviour
{
    [Header("Radar Settings")]
    public float worldRadius = 30f;       // how far (world units) the radar can see
    public float radarSize = 160f;         // UI diameter in pixels
    public float dotSize = 10f;
    public float playerDotSize = 8f;

    [Header("Colors")]
    public Color bgColor = new Color(0f, 0f, 0f, 0.25f);
    public Color borderColor = new Color(1f, 1f, 1f, 0.15f);
    public Color enemyDormantColor = new Color(1f, 0.55f, 0.5f, 0.35f);
    public Color enemyActiveColor = new Color(1f, 0.1f, 0.05f, 1f);
    public Color playerColor = new Color(1f, 1f, 1f, 0.6f);
    public Color portalColor = new Color(0.3f, 1f, 0.5f, 1f);
    public float portalDotSize = 14f;

    [Header("Room Layout")]
    public Color wallColor = new Color(0.6f, 0.65f, 0.7f, 0.4f);
    public Color floorColor = new Color(0.3f, 0.35f, 0.4f, 0.15f);

    // Internal
    private RectTransform radarRect;
    private Image radarBg;
    private RectTransform playerDot;

    private List<TrackedDot> enemyDots = new List<TrackedDot>();

    // Pool of reusable dot images
    private List<Image> dotPool = new List<Image>();
    private int dotPoolIndex;

    private Canvas canvas;
    private static Sprite circleSprite;

    // Room layout
    private RawImage roomLayoutImage;
    private Texture2D roomLayoutTex;
    private TilemapRoomBuilder cachedBuilder;
    private int lastBuiltRoom = -1;

    struct TrackedDot
    {
        public Transform target;
        public bool isActive; // for enemies: activated state
    }

    void Start()
    {
        BuildRadarUI();
    }

    void LateUpdate()
    {
        UpdateRoomLayout();

        dotPoolIndex = 0;

        // Enemies
        var enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            PlaceDot(enemy.transform.position, enemy.IsActivated ? enemyActiveColor : enemyDormantColor);
        }

        // Portal — pulsing green dot
        var portal = FindAnyObjectByType<RoomClearPortal>();
        if (portal != null)
        {
            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 5f);
            Color pColor = portalColor;
            pColor.a = pulse;
            PlaceDot(portal.transform.position, pColor, portalDotSize);
        }

        // Hide unused dots
        for (int i = dotPoolIndex; i < dotPool.Count; i++)
            dotPool[i].enabled = false;
    }

    void PlaceDot(Vector3 worldPos, Color color, float size = -1f)
    {
        if (size < 0f) size = dotSize;

        Vector2 offset = (Vector2)(worldPos - transform.position);
        float dist = offset.magnitude;

        float radarRadius = radarSize * 0.5f;
        float maxDotRadius = radarRadius - size * 0.5f;

        // Map world distance to radar distance
        float radarDist = (dist / worldRadius) * maxDotRadius;

        Vector2 dir = dist > 0.01f ? offset.normalized : Vector2.up;

        // Clamp to edge
        if (radarDist > maxDotRadius)
            radarDist = maxDotRadius;

        Vector2 localPos = dir * radarDist;

        Image dot = GetOrCreateDot();
        dot.enabled = true;
        dot.color = color;
        dot.rectTransform.anchoredPosition = localPos;

        // Dots at the edge pulse slightly smaller to hint "out of range"
        float scale = dist > worldRadius ? 0.7f : 1f;
        dot.rectTransform.sizeDelta = new Vector2(size * scale, size * scale);
    }

    Image GetOrCreateDot()
    {
        if (dotPoolIndex < dotPool.Count)
            return dotPool[dotPoolIndex++];

        // Create new dot
        var dotObj = new GameObject("RadarDot");
        dotObj.transform.SetParent(radarRect, false);

        var img = dotObj.AddComponent<Image>();
        img.sprite = GetCircleSprite();
        img.raycastTarget = false;
        img.rectTransform.sizeDelta = new Vector2(dotSize, dotSize);
        img.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        img.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        dotPool.Add(img);
        dotPoolIndex++;
        return img;
    }

    void BuildRadarUI()
    {
        // Canvas
        var canvasObj = new GameObject("RadarCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Radar container — bottom-right
        var radarObj = new GameObject("RadarDisc");
        radarObj.transform.SetParent(canvasObj.transform, false);

        radarRect = radarObj.AddComponent<RectTransform>();
        radarRect.anchorMin = new Vector2(1f, 0f);
        radarRect.anchorMax = new Vector2(1f, 0f);
        radarRect.pivot = new Vector2(1f, 0f);
        radarRect.anchoredPosition = new Vector2(-20f, 20f);
        radarRect.sizeDelta = new Vector2(radarSize, radarSize);

        // Background circle
        radarBg = radarObj.AddComponent<Image>();
        radarBg.sprite = GetCircleSprite();
        radarBg.color = bgColor;
        radarBg.raycastTarget = false;

        // Circular mask so dots are clipped to the circle
        var mask = radarObj.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // Border ring
        var borderObj = new GameObject("RadarBorder");
        borderObj.transform.SetParent(radarObj.transform, false);
        var borderRT = borderObj.AddComponent<RectTransform>();
        borderRT.anchorMin = Vector2.zero;
        borderRT.anchorMax = Vector2.one;
        borderRT.offsetMin = new Vector2(-2f, -2f);
        borderRT.offsetMax = new Vector2(2f, 2f);
        var borderImg = borderObj.AddComponent<Image>();
        borderImg.sprite = GetCircleSprite();
        borderImg.color = borderColor;
        borderImg.raycastTarget = false;
        // Push border behind the mask content by setting it as first child
        borderObj.transform.SetAsFirstSibling();

        // Range rings (subtle)
        CreateRangeRing(radarObj.transform, 0.33f);
        CreateRangeRing(radarObj.transform, 0.66f);

        // Room layout (rendered behind dots, clipped by mask)
        var layoutObj = new GameObject("RoomLayout");
        layoutObj.transform.SetParent(radarObj.transform, false);
        roomLayoutImage = layoutObj.AddComponent<RawImage>();
        roomLayoutImage.raycastTarget = false;
        roomLayoutImage.enabled = false;
        var layoutRT = roomLayoutImage.rectTransform;
        layoutRT.anchorMin = new Vector2(0.5f, 0.5f);
        layoutRT.anchorMax = new Vector2(0.5f, 0.5f);
        layoutRT.pivot = new Vector2(0.5f, 0.5f);

        // Player dot (center)
        var playerObj = new GameObject("PlayerDot");
        playerObj.transform.SetParent(radarObj.transform, false);
        var pImg = playerObj.AddComponent<Image>();
        pImg.sprite = GetCircleSprite();
        pImg.color = playerColor;
        pImg.raycastTarget = false;
        playerDot = playerObj.GetComponent<RectTransform>();
        playerDot.anchorMin = new Vector2(0.5f, 0.5f);
        playerDot.anchorMax = new Vector2(0.5f, 0.5f);
        playerDot.pivot = new Vector2(0.5f, 0.5f);
        playerDot.sizeDelta = new Vector2(playerDotSize, playerDotSize);
        playerDot.anchoredPosition = Vector2.zero;
    }

    void CreateRangeRing(Transform parent, float normalizedRadius)
    {
        var ringObj = new GameObject("RangeRing");
        ringObj.transform.SetParent(parent, false);
        var rt = ringObj.AddComponent<RectTransform>();
        float ringDiameter = radarSize * normalizedRadius;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(ringDiameter, ringDiameter);
        rt.anchoredPosition = Vector2.zero;

        var img = ringObj.AddComponent<Image>();
        img.sprite = GetCircleSprite();
        img.color = new Color(1f, 1f, 1f, 0.06f);
        img.raycastTarget = false;
    }

    void UpdateRoomLayout()
    {
        var dm = DungeonManager.Instance;
        if (dm == null || dm.roomBuilder == null)
        {
            if (roomLayoutImage != null) roomLayoutImage.enabled = false;
            return;
        }

        // Rebuild texture when room changes
        if (dm.CurrentRoomIndex != lastBuiltRoom)
        {
            RebuildRoomTexture(dm.roomBuilder);
            lastBuiltRoom = dm.CurrentRoomIndex;
        }

        if (roomLayoutImage == null || cachedBuilder == null) return;
        roomLayoutImage.enabled = true;

        float pixelsPerUnit = radarSize / (2f * worldRadius);
        int w = cachedBuilder.width;
        int h = cachedBuilder.height;

        // Size of room image in radar pixels
        roomLayoutImage.rectTransform.sizeDelta = new Vector2(w * pixelsPerUnit, h * pixelsPerUnit);

        // Position: center of grid in world, mapped to radar offset from player
        Vector3 gridCenter = cachedBuilder.CellToWorld(new Vector2Int(w / 2, h / 2));
        Vector2 offset = (Vector2)(gridCenter - transform.position);
        roomLayoutImage.rectTransform.anchoredPosition = offset * pixelsPerUnit;
    }

    void RebuildRoomTexture(TilemapRoomBuilder builder)
    {
        cachedBuilder = builder;
        int w = builder.width;
        int h = builder.height;
        if (w == 0 || h == 0) return;

        if (roomLayoutTex == null || roomLayoutTex.width != w || roomLayoutTex.height != h)
        {
            if (roomLayoutTex != null) Destroy(roomLayoutTex);
            roomLayoutTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            roomLayoutTex.filterMode = FilterMode.Point;
            roomLayoutTex.wrapMode = TextureWrapMode.Clamp;
        }

        Color clear = Color.clear;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (builder.IsFloor(x, y))
                {
                    roomLayoutTex.SetPixel(x, y, floorColor);
                }
                else if (IsAdjacentToFloor(builder, x, y, w, h))
                {
                    roomLayoutTex.SetPixel(x, y, wallColor);
                }
                else
                {
                    roomLayoutTex.SetPixel(x, y, clear);
                }
            }
        }

        roomLayoutTex.Apply();
        if (roomLayoutImage != null)
            roomLayoutImage.texture = roomLayoutTex;
    }

    bool IsAdjacentToFloor(TilemapRoomBuilder builder, int x, int y, int w, int h)
    {
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx, ny = y + dy;
            if (nx >= 0 && nx < w && ny >= 0 && ny < h && builder.IsFloor(nx, ny))
                return true;
        }
        return false;
    }

    void OnDestroy()
    {
        if (roomLayoutTex != null) Destroy(roomLayoutTex);
    }

    static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float radius = center - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float a = Mathf.Clamp01(1f - ((dist - radius + 1f)));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }
}
