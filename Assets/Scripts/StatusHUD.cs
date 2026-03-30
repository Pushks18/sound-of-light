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

    private TextMeshProUGUI hpLabel;
    private TextMeshProUGUI flashLabel;
    private TextMeshProUGUI enemyLabel;

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
        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null)
        {
            Debug.LogError("StatusHUD: Could not find GameObject named 'Canvas'.");
            enabled = false;
            return;
        }

        hpLabel = FindTMP(canvasGO.transform, "StatusContainer/HP");
        flashLabel = FindTMP(canvasGO.transform, "StatusContainer/Flash");
        enemyLabel = FindTMP(canvasGO.transform, "StatusContainer/Enemies");
    }

    TextMeshProUGUI FindTMP(Transform root, string path)
    {
        var t = root.Find(path);
        if (t == null)
        {
            Debug.LogError($"StatusHUD: Could not find path '{path}' under Canvas.");
            return null;
        }

        var tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
            Debug.LogError($"StatusHUD: '{path}' has no TextMeshProUGUI component.");

        return tmp;
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