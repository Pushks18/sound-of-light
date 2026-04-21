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

    [Header("Ability Cooldowns")]
    public float bulletCooldown = 0.1f;
    public float dashCooldown = 1.0f;
    public float flashCooldown = 4.0f;

    [Header("Bullet Regen")]
    public float bulletRegenDelay = 1.5f;
    public float bulletRegenInterval = 0.12f;

    [Header("Dash Regen")]
    public float dashRegenDelay = 8f;
    public float dashRegenInterval = 2f;

    [Header("Flash Regen")]
    public float flashRegenDelay = 8f;
    public float flashRegenInterval = 2f;

    private float lastBulletUseTime = Mathf.NegativeInfinity;
    private float lastDashUseTime = Mathf.NegativeInfinity;
    private float lastFlashUseTime = Mathf.NegativeInfinity;

    private float nextBulletUseTime = Mathf.NegativeInfinity;
    private float nextDashUseTime = Mathf.NegativeInfinity;
    private float nextFlashUseTime = Mathf.NegativeInfinity;

    private float bulletRegenAccum;
    private float dashRegenAccum;
    private float flashRegenAccum;

    // HUD dirty-flag tracking — skip string formatting when nothing changed
    private int prevBullets, prevDashes, prevFlashes;
    private bool prevHadCooldown;

    // Cached color hex strings (never change)
    private static string hexKey;
    private static string hexAvailable;
    private static string hexEmpty;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Bullets = maxBullets;
        Dashes = maxDashes;
        Flashes = maxFlashes;

        if (hexKey == null)
        {
            hexKey = ColorUtility.ToHtmlStringRGB(ColKey);
            hexAvailable = ColorUtility.ToHtmlStringRGB(ColAvailable);
            hexEmpty = ColorUtility.ToHtmlStringRGB(ColEmpty);
        }
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

        // Only rebuild HUD strings when values actually changed or cooldown is ticking
        bool hasCooldown = Time.time < nextBulletUseTime || Time.time < nextDashUseTime || Time.time < nextFlashUseTime;
        if (Bullets != prevBullets || Dashes != prevDashes || Flashes != prevFlashes
            || hasCooldown || prevHadCooldown)
        {
            RefreshHUD();
            prevBullets = Bullets;
            prevDashes = Dashes;
            prevFlashes = Flashes;
            prevHadCooldown = hasCooldown;
        }
    }

    public bool TrySpendBullet()
    {
        if (Time.time < nextBulletUseTime) return false;
        if (Bullets <= 0) return false;

        Bullets--;
        lastBulletUseTime = Time.time;
        nextBulletUseTime = Time.time + bulletCooldown;
        bulletRegenAccum = 0f;

        RefreshHUD();
        return true;
    }

    public bool TrySpendDash()
    {
        if (Time.time < nextDashUseTime) return false;
        if (Dashes <= 0) return false;

        Dashes--;
        lastDashUseTime = Time.time;
        nextDashUseTime = Time.time + dashCooldown;
        dashRegenAccum = 0f;

        RefreshHUD();
        return true;
    }

    public bool TrySpendFlash()
    {
        if (Time.time < nextFlashUseTime) return false;
        if (Flashes <= 0) return false;

        Flashes--;
        lastFlashUseTime = Time.time;
        nextFlashUseTime = Time.time + flashCooldown;
        flashRegenAccum = 0f;

        RefreshHUD();
        return true;
    }

    public void AddMaxFlashes(int amount, bool refillToMax = true)
    {
        if (amount <= 0) return;

        maxFlashes += amount;
        Flashes = refillToMax
            ? maxFlashes
            : Mathf.Min(Flashes + amount, maxFlashes);

        RefreshHUD();
    }

    void BindHUD()
    {
        if (bulletLabel != null && dashLabel != null && flashLabel != null) return;

        var all = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        foreach (var t in all)
        {
            string n = t.gameObject.name;
            if (bulletLabel == null && n == "Bullets") bulletLabel = t;
            if (dashLabel == null && n == "Dash") dashLabel = t;
            if (flashLabel == null && n == "Flash" && t.transform.parent?.name == "AmmoContainer") flashLabel = t;
        }

        if (bulletLabel == null) Debug.LogWarning("PlayerAmmo: Could not find Bullets label.");
        if (dashLabel == null) Debug.LogWarning("PlayerAmmo: Could not find Dash label.");
        if (flashLabel == null) Debug.LogWarning("PlayerAmmo: Could not find Flash label.");
    }

    void RefreshHUD()
    {
        if (bulletLabel != null)
            bulletLabel.text = FormatRow("[K] Bullets", Bullets, maxBullets, GetCooldownRemaining(nextBulletUseTime));

        if (dashLabel != null)
            dashLabel.text = FormatRow("[Shift] Dash", Dashes, maxDashes, GetCooldownRemaining(nextDashUseTime));

        if (flashLabel != null)
            flashLabel.text = FormatRow("[L] Flash", Flashes, maxFlashes, GetCooldownRemaining(nextFlashUseTime));
    }

    float GetCooldownRemaining(float nextUseTime)
    {
        return Mathf.Max(0f, nextUseTime - Time.time);
    }

    string FormatRow(string label, int current, int max, float cooldownRemaining)
    {
        if (cooldownRemaining > 0f)
        {
            string time = cooldownRemaining.ToString("0.00");
            return $"<color=#FFFFFF>{time}</color> <color=#{hexKey}>{label}</color> ";
        }

        string hex = current > 0 ? hexAvailable : hexEmpty;
        string pips = BuildPips(current, max);

        return $"<color=#{hex}>{pips}</color> <color=#{hexKey}>{label}</color>  ";
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

    public void AdvanceFlashCooldown(float amount)
    {
        if (amount <= 0f) return;

        // If no cooldown is active, do nothing
        if (Time.time >= nextFlashUseTime) return;

        // Reduce remaining cooldown
        nextFlashUseTime -= amount;

        // Clamp so it doesn't go into the past unnecessarily
        if (nextFlashUseTime < Time.time)
            nextFlashUseTime = Time.time;

        RefreshHUD();
    }
}