using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunKillAnalytics : MonoBehaviour
{
    public const string DamageMethodBullet = "bullet";
    public const string DamageMethodSlash = "slash";
    public const string DamageMethodTrap = "trap";
    public const string DamageMethodDash = "dash";
    public const string DamageMethodUnknown = "unknown";

    public static RunKillAnalytics Instance { get; private set; }

    private readonly Dictionary<string, int> killCounts = new Dictionary<string, int>();
    private long sessionID;
    private bool hasSentRunSummary;
    private SendToGoogle sendToGoogle;
    private bool interactionActive;
    private float interactionStartTime;
    private float lastPlayerDamageTime;
    private int currentInteractionDamage;
    private int interactionCount;
    private const float InteractionTimeoutSeconds = 5f;
    private int currentLevelIndex;
    private int highestLevelReached;
    private float currentLevelStartTime;
    private bool hasSentCurrentLevelClear;

    public long SessionID => sessionID;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        sessionID = DateTime.Now.Ticks;
        sendToGoogle = GetComponent<SendToGoogle>();
        ResetCounts();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        TrackLevelProgress();

        if (!interactionActive)
            return;

        if (Time.time - lastPlayerDamageTime >= InteractionTimeoutSeconds)
            FinalizeDamageInteraction("timeout", lastPlayerDamageTime);
    }

    public void RecordEnemyKill(string damageMethod)
    {
        string key = NormalizeMethod(damageMethod);
        killCounts[key]++;
    }

    public void RecordPlayerDamageTaken(int damage)
    {
        if (damage <= 0)
            return;

        if (!interactionActive)
        {
            interactionActive = true;
            interactionStartTime = Time.time;
            currentInteractionDamage = 0;
            interactionCount++;
        }

        currentInteractionDamage += damage;
        lastPlayerDamageTime = Time.time;
    }

    public void RecordEnemyTimeToKill(string enemyName, float timeToKillSeconds, IEnumerable<string> damageTypesUsed, string killMethod)
    {
        if (sendToGoogle == null)
            sendToGoogle = GetComponent<SendToGoogle>() ?? FindAnyObjectByType<SendToGoogle>();

        if (sendToGoogle == null)
        {
            Debug.LogWarning("RunKillAnalytics could not find SendToGoogle. Enemy TTK event was not sent.");
            return;
        }

        string normalizedKillMethod = NormalizeMethod(killMethod);
        List<string> normalizedDamageTypes = new List<string>();

        if (damageTypesUsed != null)
        {
            foreach (string damageType in damageTypesUsed)
            {
                string normalized = NormalizeMethod(damageType);
                if (!normalizedDamageTypes.Contains(normalized))
                    normalizedDamageTypes.Add(normalized);
            }
        }

        if (normalizedDamageTypes.Count == 0)
            normalizedDamageTypes.Add(DamageMethodUnknown);

        sendToGoogle.SendEnemyTimeToKill(
            sessionID,
            enemyName,
            timeToKillSeconds,
            normalizedKillMethod,
            string.Join(",", normalizedDamageTypes.OrderBy(value => value)));
    }

    public void SendRunSummary(string runOutcome)
    {
        TrackLevelProgress();

        if (interactionActive)
            FinalizeDamageInteraction(runOutcome, Time.time);

        if (hasSentRunSummary)
            return;

        hasSentRunSummary = true;

        if (sendToGoogle == null)
            sendToGoogle = GetComponent<SendToGoogle>() ?? FindAnyObjectByType<SendToGoogle>();

        if (sendToGoogle == null)
        {
            Debug.LogWarning("RunKillAnalytics could not find SendToGoogle. Run summary was not sent.");
            return;
        }

        if (string.Equals(runOutcome, "win", StringComparison.OrdinalIgnoreCase) && !hasSentCurrentLevelClear)
            SendCurrentLevelClear("win");

        int deathLevel = string.Equals(runOutcome, "death", StringComparison.OrdinalIgnoreCase)
            ? GetResolvedLevelIndex()
            : 0;

        sendToGoogle.SendRunKillSummary(
            sessionID,
            runOutcome,
            new Dictionary<string, int>(killCounts),
            highestLevelReached,
            deathLevel);
    }

    public void RecordCurrentLevelCleared(string clearResult = "cleared")
    {
        TrackLevelProgress();
        SendCurrentLevelClear(clearResult);
    }

    public int GetKillCount(string damageMethod)
    {
        string key = NormalizeMethod(damageMethod);
        return killCounts.TryGetValue(key, out int count) ? count : 0;
    }

    public int GetTotalKills()
    {
        int total = 0;
        foreach (var pair in killCounts)
            total += pair.Value;
        return total;
    }

    void ResetCounts()
    {
        killCounts.Clear();
        killCounts[DamageMethodBullet] = 0;
        killCounts[DamageMethodSlash] = 0;
        killCounts[DamageMethodTrap] = 0;
        killCounts[DamageMethodDash] = 0;
        killCounts[DamageMethodUnknown] = 0;
    }

    void FinalizeDamageInteraction(string endReason, float effectiveEndTime)
    {
        if (!interactionActive)
            return;

        if (sendToGoogle == null)
            sendToGoogle = GetComponent<SendToGoogle>() ?? FindAnyObjectByType<SendToGoogle>();

        if (sendToGoogle == null)
        {
            Debug.LogWarning("RunKillAnalytics could not find SendToGoogle. Damage interaction event was not sent.");
            interactionActive = false;
            currentInteractionDamage = 0;
            return;
        }

        float durationSeconds = Mathf.Max(0f, effectiveEndTime - interactionStartTime);
        sendToGoogle.SendDamageInteraction(
            sessionID,
            interactionCount,
            currentInteractionDamage,
            durationSeconds,
            endReason);

        interactionActive = false;
        currentInteractionDamage = 0;
        interactionStartTime = 0f;
        lastPlayerDamageTime = 0f;
    }

    void TrackLevelProgress()
    {
        int resolvedLevelIndex = GetResolvedLevelIndex();
        if (resolvedLevelIndex <= 0)
            return;

        if (resolvedLevelIndex != currentLevelIndex)
        {
            currentLevelIndex = resolvedLevelIndex;
            highestLevelReached = Mathf.Max(highestLevelReached, currentLevelIndex);
            currentLevelStartTime = Time.time;
            hasSentCurrentLevelClear = false;
        }

        if (hasSentCurrentLevelClear)
            return;

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || gameManager.gameEnded || gameManager.enemyCount > 0)
            return;

        SendCurrentLevelClear("cleared");
    }

    void SendCurrentLevelClear(string clearResult)
    {
        if (currentLevelIndex <= 0 || hasSentCurrentLevelClear)
            return;

        if (sendToGoogle == null)
            sendToGoogle = GetComponent<SendToGoogle>() ?? FindAnyObjectByType<SendToGoogle>();

        if (sendToGoogle == null)
        {
            Debug.LogWarning("RunKillAnalytics could not find SendToGoogle. Room clear event was not sent.");
            return;
        }

        float clearDurationSeconds = Mathf.Max(0f, Time.time - currentLevelStartTime);
        bool isBossLevel = GameManager.Instance != null && GameManager.Instance.isBossFight;

        sendToGoogle.SendRoomClear(
            sessionID,
            currentLevelIndex,
            clearDurationSeconds,
            isBossLevel,
            clearResult);

        hasSentCurrentLevelClear = true;
    }

    int GetResolvedLevelIndex()
    {
        if (DungeonManager.Instance != null)
            return DungeonManager.Instance.CurrentRoomIndex;

        int sceneLevelIndex = TryGetSceneLevelIndex();
        if (sceneLevelIndex > 0)
            return sceneLevelIndex;

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.isBossFight && DungeonManager.RoomIndexBeforeBoss > 0)
                return DungeonManager.RoomIndexBeforeBoss;

            return highestLevelReached > 0 ? highestLevelReached : 1;
        }

        return 0;
    }

    int TryGetSceneLevelIndex()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrWhiteSpace(sceneName))
            return 0;

        if (sceneName.StartsWith("Level", StringComparison.OrdinalIgnoreCase))
        {
            string suffix = sceneName.Substring("Level".Length).Trim();
            if (int.TryParse(suffix, out int parsedLevelIndex))
                return parsedLevelIndex;
        }

        return 0;
    }

    string NormalizeMethod(string damageMethod)
    {
        if (string.IsNullOrWhiteSpace(damageMethod))
            return DamageMethodUnknown;

        string normalized = damageMethod.Trim().ToLowerInvariant();
        if (!killCounts.ContainsKey(normalized))
            return DamageMethodUnknown;

        return normalized;
    }
}
