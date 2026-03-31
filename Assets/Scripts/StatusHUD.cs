using UnityEngine;
using TMPro;

public class StatusHUD : MonoBehaviour
{
    public static StatusHUD Instance { get; private set; }

    private int currentHP = 5;
    private int maxHP = 5;
    private float flashCooldown = 0f;
    private float flashMaxCD = 20f;
    private int enemiesLeft = 0;

    [Header("HUD Labels (assign in Inspector, or leave blank to auto-find by name)")]
    [SerializeField] private TextMeshProUGUI hpLabel;
    [SerializeField] private TextMeshProUGUI flashLabel;
    [SerializeField] private TextMeshProUGUI enemyLabel;

    private static readonly Color ColGreen  = new Color(0.35f, 1f,   0.45f, 1f);
    private static readonly Color ColYellow = new Color(1f,   0.85f, 0.2f,  1f);
    private static readonly Color ColRed    = new Color(1f,   0.25f, 0.2f,  1f);
    private static readonly Color ColReady  = new Color(0.4f, 0.9f,  1f,   1f);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        BindHUD();
        RefreshHP();
        RefreshFlash();
        RefreshEnemy();
        UpdateEnemies();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (flashCooldown > 0f)
        {
            flashCooldown -= Time.deltaTime;
            if (flashCooldown < 0f) flashCooldown = 0f;
            RefreshFlash();
        }
    }

    public void UpdateHP(int current, int max = -1)
    {
        currentHP = current;
        if (max > 0) maxHP = max;
        RefreshHP();
    }

    public void StartFlashCooldown(float cooldownDuration)
    {
        flashCooldown = cooldownDuration;
        flashMaxCD = cooldownDuration;
        RefreshFlash();
    }

    public void UpdateEnemies()
    {
        enemiesLeft = GameObject.FindGameObjectsWithTag("Enemy").Length;
        RefreshEnemy();
    }

    public void DecrementEnemies()
    {
        enemiesLeft -= 1;
        RefreshEnemy();
    }

    void BindHUD()
    {
        // If all three are already assigned in Inspector, nothing to do
        if (hpLabel != null && flashLabel != null && enemyLabel != null) return;

        // Fallback: search all TMP components in the scene by GameObject name
        var all = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        foreach (var t in all)
        {
            string n = t.gameObject.name;
            if (hpLabel    == null && n == "HP")      hpLabel    = t;
            if (flashLabel == null && n == "Flash" && t.transform.parent?.name == "StatusContainer") flashLabel = t;
            if (enemyLabel == null && n == "Enemies") enemyLabel = t;
        }

        if (hpLabel    == null) Debug.LogWarning("StatusHUD: Could not find HP label.");
        if (flashLabel == null) Debug.LogWarning("StatusHUD: Could not find Flash label.");
        if (enemyLabel == null) Debug.LogWarning("StatusHUD: Could not find Enemies label.");
    }

    void RefreshHP()
    {
        if (hpLabel == null) return;

        Color c = currentHP >= maxHP ? ColGreen : (currentHP > maxHP / 2 ? ColYellow : ColRed);
        string hex = ColorUtility.ToHtmlStringRGB(c);
        string pips = BuildPips(currentHP, maxHP, "♥", "♡");

        hpLabel.text = $"<b>HP</b>  <color=#{hex}>{pips}</color>";
    }

    void RefreshFlash()
    {
        if (flashLabel == null) return;

        if (flashCooldown <= 0f)
        {
            string readyHex = ColorUtility.ToHtmlStringRGB(ColReady);
            flashLabel.text = $"<b>Flash</b>  <color=#{readyHex}>READY</color>";
        }
        else
        {
            string cdHex = ColorUtility.ToHtmlStringRGB(ColRed);
            int filled = Mathf.RoundToInt((1f - flashCooldown / flashMaxCD) * 10f);
            string bar = BuildPips(filled, 10, "■", "□");
            flashLabel.text = $"<b>Flash</b>  <color=#{cdHex}>{bar} {flashCooldown:F1}s</color>";
        }
    }

    void RefreshEnemy()
    {
        if (enemyLabel == null) return;

        Color c = enemiesLeft <= 0 ? ColGreen : ColYellow;
        string hex = ColorUtility.ToHtmlStringRGB(c);
        string txt = enemiesLeft <= 0 ? "All clear!" : enemiesLeft.ToString();
        enemyLabel.text = $"<b>Enemies</b>  <color=#{hex}>{txt}</color>";
    }

    string BuildPips(int current, int max, string full, string empty)
    {
        empty = "<color=#555555>" + empty + "</color>";
        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < max; i++)
            sb.Append(i < current ? full : empty);

        return sb.ToString();
    }
}