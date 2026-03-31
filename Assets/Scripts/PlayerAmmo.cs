using UnityEngine;
using TMPro;

public class PlayerAmmo : MonoBehaviour
{
    public static PlayerAmmo Instance { get; private set; }

    [Header("Starting Counts")]
    public int maxBullets = 15;
    public int maxDashes = 5;
    public int maxFlashes = 5;

    public int Bullets { get; private set; }
    public int Dashes  { get; private set; }
    public int Flashes { get; private set; }

    [Header("HUD Labels (assign in Inspector, or leave blank to auto-find by name)")]
    [SerializeField] private TextMeshProUGUI bulletLabel;
    [SerializeField] private TextMeshProUGUI dashLabel;
    [SerializeField] private TextMeshProUGUI flashLabel;

    private static readonly Color ColAvailable = new Color(1f, 1f, 1f, 1f);
    private static readonly Color ColEmpty = new Color(1f, 0.3f, 0.3f, 0.8f);
    private static readonly Color ColKey = new Color(1f, 0.85f, 0.2f, 1f);

    [Header("Bullet Regen")]
    public float bulletRegenDelay = 5f;
    public float bulletRegenInterval = 0.5f;

    [Header("Dash Regen")]
    public float dashRegenDelay = 8f;
    public float dashRegenInterval = 2f;

    [Header("Flash Regen")]
    public float flashRegenDelay = 8f;
    public float flashRegenInterval = 2f;

    private float lastBulletUseTime = Mathf.NegativeInfinity;
    private float lastDashUseTime = Mathf.NegativeInfinity;
    private float lastFlashUseTime = Mathf.NegativeInfinity;

    private float bulletRegenAccum;
    private float dashRegenAccum;
    private float flashRegenAccum;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Bullets = maxBullets;
        Dashes = maxDashes;
        Flashes = maxFlashes;
    }

    void Start()
    {
        BindHUD();
        RefreshHUD();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (Bullets < maxBullets && Time.time - lastBulletUseTime >= bulletRegenDelay)
        {
            bulletRegenAccum += Time.deltaTime;
            while (bulletRegenAccum >= bulletRegenInterval && Bullets < maxBullets)
            {
                bulletRegenAccum -= bulletRegenInterval;
                Bullets++;
            }
        }

        if (Dashes < maxDashes && Time.time - lastDashUseTime >= dashRegenDelay)
        {
            dashRegenAccum += Time.deltaTime;
            while (dashRegenAccum >= dashRegenInterval && Dashes < maxDashes)
            {
                dashRegenAccum -= dashRegenInterval;
                Dashes++;
            }
        }

        if (Flashes < maxFlashes && Time.time - lastFlashUseTime >= flashRegenDelay)
        {
            flashRegenAccum += Time.deltaTime;
            while (flashRegenAccum >= flashRegenInterval && Flashes < maxFlashes)
            {
                flashRegenAccum -= flashRegenInterval;
                Flashes++;
            }
        }

        if (Bullets < maxBullets || Dashes < maxDashes || Flashes < maxFlashes)
            RefreshHUD();
    }

    public bool TrySpendBullet()
    {
        if (Bullets <= 0) return false;
        Bullets--;
        lastBulletUseTime = Time.time;
        bulletRegenAccum = 0f;
        RefreshHUD();
        return true;
    }

    public bool TrySpendDash()
    {
        if (Dashes <= 0) return false;
        Dashes--;
        lastDashUseTime = Time.time;
        dashRegenAccum = 0f;
        RefreshHUD();
        return true;
    }

    public bool TrySpendFlash()
    {
        if (Flashes <= 0) return false;
        Flashes--;
        lastFlashUseTime = Time.time;
        flashRegenAccum = 0f;
        RefreshHUD();
        return true;
    }

    void BindHUD()
    {
        // If all three are already assigned in Inspector, nothing to do
        if (bulletLabel != null && dashLabel != null && flashLabel != null) return;

        // Fallback: search all TMP components in the scene by GameObject name
        var all = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        foreach (var t in all)
        {
            string n = t.gameObject.name;
            if (bulletLabel == null && n == "Bullets") bulletLabel = t;
            if (dashLabel   == null && n == "Dash")    dashLabel   = t;
            if (flashLabel  == null && n == "Flash" && t.transform.parent?.name == "AmmoContainer") flashLabel = t;
        }

        if (bulletLabel == null) Debug.LogWarning("PlayerAmmo: Could not find Bullets label.");
        if (dashLabel   == null) Debug.LogWarning("PlayerAmmo: Could not find Dash label.");
        if (flashLabel  == null) Debug.LogWarning("PlayerAmmo: Could not find Flash label.");
    }

    void RefreshHUD()
    {
        if (bulletLabel != null)
            bulletLabel.text = FormatRow("[K] Bullets", Bullets, maxBullets);

        if (dashLabel != null)
            dashLabel.text = FormatRow("[Shift] Dash", Dashes, maxDashes);

        if (flashLabel != null)
            flashLabel.text = FormatRow("[L] Flash", Flashes, maxFlashes);
    }

    string FormatRow(string label, int current, int max)
    {
        Color c = current > 0 ? ColAvailable : ColEmpty;
        string hex = ColorUtility.ToHtmlStringRGB(c);
        string keyHex = ColorUtility.ToHtmlStringRGB(ColKey);
        string pips = BuildPips(current, max);

        return $"<color=#{hex}>{pips}</color> <color=#{keyHex}>{label}</color>  ";
    }

    string BuildPips(int current, int max)
    {
        string full = "<color=#FFE066>■</color>";
        string empty = "<color=#555555>□</color>";
        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < max; i++)
            sb.Append(i < current ? full : empty);

        return sb.ToString();
    }
}