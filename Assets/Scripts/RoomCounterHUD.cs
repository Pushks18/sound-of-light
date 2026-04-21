using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Displays "Room X" at the top-center of the screen in endless mode.
/// Builds its own UI — no prefab needed. Attach to any persistent GameObject
/// in the RoomGenScene, or add to the Player.
/// </summary>
public class RoomCounterHUD : MonoBehaviour
{
    public static RoomCounterHUD Instance { get; private set; }

    [Header("Appearance")]
    public float fontSize = 28f;
    public float showDuration = 3f;
    public float fadeDuration = 1f;

    private TextMeshProUGUI label;
    private CanvasGroup canvasGroup;
    private Coroutine fadeRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildHUD();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void UpdateRoom(int roomNumber)
    {
        if (label == null) return;

        label.text = "Room " + roomNumber;

        // Show with fade-in, hold, then fade-out
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(ShowAndFade());
    }

    IEnumerator ShowAndFade()
    {
        // Fade in
        float elapsed = 0f;
        float fadeIn = 0.3f;
        canvasGroup.alpha = 0f;
        while (elapsed < fadeIn)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeIn);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Hold
        yield return new WaitForSecondsRealtime(showDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }

    void BuildHUD()
    {
        var canvasObj = new GameObject("RoomCounterCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 58;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Label — top center
        var labelObj = new GameObject("RoomLabel");
        labelObj.transform.SetParent(canvasObj.transform, false);

        var rt = labelObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -60f);
        rt.sizeDelta = new Vector2(300f, 50f);

        label = labelObj.AddComponent<TextMeshProUGUI>();
        var font = TMP_Settings.defaultFontAsset;
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null)
            label.font = font;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.95f, 0.7f, 1f);
        label.raycastTarget = false;
        label.text = "";
    }
}
