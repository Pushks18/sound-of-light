using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SendToGoogle : MonoBehaviour
{
    private const string DefaultFormUrl = "https://docs.google.com/forms/d/e/1FAIpQLSdMdclQh_ml9uNfl1QDL6ylG4KMJijfRgLQQvNSZe2ifryu2Q/formResponse";
    private const string DefaultRunSessionIdEntry = "entry.1836047267";
    private const string DefaultRunOutcomeEntry = "entry.13699829";
    private const string DefaultTotalKillsEntry = "entry.316910690";
    private const string DefaultBulletKillsEntry = "entry.1275383987";
    private const string DefaultSlashKillsEntry = "entry.2118033398";
    private const string DefaultTrapKillsEntry = "entry.141215274";
    private const string DefaultDashKillsEntry = "entry.705370448";
    private const string DefaultUnknownKillsEntry = "entry.429632131";
    private const string DefaultHighestLevelReachedEntry = "entry.1164402421";
    private const string DefaultDeathLevelEntry = "entry.490526072";
    private const string DefaultTtkSessionIdEntry = "entry.1445909754";
    private const string DefaultTtkEnemyNameEntry = "entry.679796372";
    private const string DefaultTtkSecondsEntry = "entry.588633834";
    private const string DefaultTtkKillMethodEntry = "entry.2135333005";
    private const string DefaultTtkDamageTypesEntry = "entry.556402948";
    private const string DefaultRoomClearSessionIdEntry = "entry.693650357";
    private const string DefaultRoomClearLevelEntry = "entry.588434780";
    private const string DefaultRoomClearSecondsEntry = "entry.485085220";
    private const string DefaultRoomClearIsBossEntry = "entry.186545810";
    private const string DefaultRoomClearResultEntry = "entry.1676784979";
    private const string DefaultInteractionSessionIdEntry = "entry.1288133725";
    private const string DefaultInteractionIndexEntry = "entry.1218770745";
    private const string DefaultInteractionDamageTakenEntry = "entry.1618313918";
    private const string DefaultInteractionDurationSecondsEntry = "entry.585922166";
    private const string DefaultInteractionEndReasonEntry = "entry.1208376492";

    [SerializeField] private string URL = DefaultFormUrl;
    [SerializeField] private string deathPositionEntry = "";
    [Header("Run Kill Summary Entries")]
    [SerializeField] private string runSessionIdEntry = DefaultRunSessionIdEntry;
    [SerializeField] private string runOutcomeEntry = DefaultRunOutcomeEntry;
    [SerializeField] private string totalKillsEntry = DefaultTotalKillsEntry;
    [SerializeField] private string bulletKillsEntry = DefaultBulletKillsEntry;
    [SerializeField] private string slashKillsEntry = DefaultSlashKillsEntry;
    [SerializeField] private string trapKillsEntry = DefaultTrapKillsEntry;
    [SerializeField] private string dashKillsEntry = DefaultDashKillsEntry;
    [SerializeField] private string unknownKillsEntry = DefaultUnknownKillsEntry;
    [SerializeField] private string highestLevelReachedEntry = DefaultHighestLevelReachedEntry;
    [SerializeField] private string deathLevelEntry = DefaultDeathLevelEntry;
    [Header("Enemy Time To Kill Entries")]
    [SerializeField] private string ttkSessionIdEntry = DefaultTtkSessionIdEntry;
    [SerializeField] private string ttkEnemyNameEntry = DefaultTtkEnemyNameEntry;
    [SerializeField] private string ttkSecondsEntry = DefaultTtkSecondsEntry;
    [SerializeField] private string ttkKillMethodEntry = DefaultTtkKillMethodEntry;
    [SerializeField] private string ttkDamageTypesEntry = DefaultTtkDamageTypesEntry;
    [Header("Room Clear Entries")]
    [SerializeField] private string roomClearSessionIdEntry = DefaultRoomClearSessionIdEntry;
    [SerializeField] private string roomClearLevelEntry = DefaultRoomClearLevelEntry;
    [SerializeField] private string roomClearSecondsEntry = DefaultRoomClearSecondsEntry;
    [SerializeField] private string roomClearIsBossEntry = DefaultRoomClearIsBossEntry;
    [SerializeField] private string roomClearResultEntry = DefaultRoomClearResultEntry;
    [Header("Damage Interaction Entries")]
    [SerializeField] private string interactionSessionIdEntry = DefaultInteractionSessionIdEntry;
    [SerializeField] private string interactionIndexEntry = DefaultInteractionIndexEntry;
    [SerializeField] private string interactionDamageTakenEntry = DefaultInteractionDamageTakenEntry;
    [SerializeField] private string interactionDurationSecondsEntry = DefaultInteractionDurationSecondsEntry;
    [SerializeField] private string interactionEndReasonEntry = DefaultInteractionEndReasonEntry;

    private long _sessionID;

    private void Awake()
    {
        ApplyDefaultAnalyticsConfigIfMissing();
        // Assign sessionID to identify playtests
        _sessionID = DateTime.Now.Ticks;
    }

    private void OnValidate()
    {
        ApplyDefaultAnalyticsConfigIfMissing();
    }

    public void SendDeathPosition(Vector3 deathPosition)
    {
        ApplyDefaultAnalyticsConfigIfMissing();

        string deathPositionString = $"{deathPosition.x:F3}, {deathPosition.y:F3}, {deathPosition.z:F3}";
        WWWForm form = new WWWForm();
        TryAddField(form, deathPositionEntry, deathPositionString);
        StartCoroutine(PostForm(form, $"Death position uploaded (session {_sessionID}): {deathPositionString}"));
    }

    public void SendRunKillSummary(long sessionID, string runOutcome, Dictionary<string, int> killCounts, int highestLevelReached, int deathLevel)
    {
        ApplyDefaultAnalyticsConfigIfMissing();

        int totalKills =
            GetKillCount(killCounts, RunKillAnalytics.DamageMethodBullet) +
            GetKillCount(killCounts, RunKillAnalytics.DamageMethodSlash) +
            GetKillCount(killCounts, RunKillAnalytics.DamageMethodTrap) +
            GetKillCount(killCounts, RunKillAnalytics.DamageMethodDash) +
            GetKillCount(killCounts, RunKillAnalytics.DamageMethodUnknown);

        WWWForm form = new WWWForm();
        TryAddField(form, runSessionIdEntry, sessionID.ToString());
        TryAddField(form, runOutcomeEntry, runOutcome);
        TryAddField(form, totalKillsEntry, totalKills.ToString());
        TryAddField(form, bulletKillsEntry, GetKillCount(killCounts, RunKillAnalytics.DamageMethodBullet).ToString());
        TryAddField(form, slashKillsEntry, GetKillCount(killCounts, RunKillAnalytics.DamageMethodSlash).ToString());
        TryAddField(form, trapKillsEntry, GetKillCount(killCounts, RunKillAnalytics.DamageMethodTrap).ToString());
        TryAddField(form, dashKillsEntry, GetKillCount(killCounts, RunKillAnalytics.DamageMethodDash).ToString());
        TryAddField(form, unknownKillsEntry, GetKillCount(killCounts, RunKillAnalytics.DamageMethodUnknown).ToString());
        TryAddField(form, highestLevelReachedEntry, highestLevelReached.ToString());
        TryAddField(form, deathLevelEntry, deathLevel > 0 ? deathLevel.ToString() : string.Empty);

        string logMessage =
            $"Run kill summary uploaded (session {sessionID}): outcome={runOutcome}, " +
            $"bullet={GetKillCount(killCounts, RunKillAnalytics.DamageMethodBullet)}, " +
            $"slash={GetKillCount(killCounts, RunKillAnalytics.DamageMethodSlash)}, " +
            $"trap={GetKillCount(killCounts, RunKillAnalytics.DamageMethodTrap)}, " +
            $"dash={GetKillCount(killCounts, RunKillAnalytics.DamageMethodDash)}, " +
            $"unknown={GetKillCount(killCounts, RunKillAnalytics.DamageMethodUnknown)}, " +
            $"highestLevelReached={highestLevelReached}, deathLevel={deathLevel}";

        StartCoroutine(PostForm(form, logMessage));
    }

    public void SendEnemyTimeToKill(long sessionID, string enemyName, float timeToKillSeconds, string killMethod, string damageTypesUsed)
    {
        ApplyDefaultAnalyticsConfigIfMissing();

        WWWForm form = new WWWForm();
        TryAddField(form, ttkSessionIdEntry, sessionID.ToString());
        TryAddField(form, ttkEnemyNameEntry, enemyName);
        TryAddField(form, ttkSecondsEntry, timeToKillSeconds.ToString("F3"));
        TryAddField(form, ttkKillMethodEntry, killMethod);
        TryAddField(form, ttkDamageTypesEntry, damageTypesUsed);

        string logMessage =
            $"Enemy TTK uploaded (session {sessionID}): enemy={enemyName}, " +
            $"ttk={timeToKillSeconds:F3}s, killMethod={killMethod}, damageTypes={damageTypesUsed}";

        StartCoroutine(PostForm(form, logMessage));
    }

    public void SendRoomClear(long sessionID, int roomLevel, float clearSeconds, bool isBossRoom, string clearResult)
    {
        ApplyDefaultAnalyticsConfigIfMissing();

        WWWForm form = new WWWForm();
        TryAddField(form, roomClearSessionIdEntry, sessionID.ToString());
        TryAddField(form, roomClearLevelEntry, roomLevel.ToString());
        TryAddField(form, roomClearSecondsEntry, clearSeconds.ToString("F3"));
        TryAddField(form, roomClearIsBossEntry, isBossRoom ? "true" : "false");
        TryAddField(form, roomClearResultEntry, clearResult);

        string logMessage =
            $"Room clear uploaded (session {sessionID}): level={roomLevel}, " +
            $"clearSeconds={clearSeconds:F3}, isBossRoom={isBossRoom}, clearResult={clearResult}";

        StartCoroutine(PostForm(form, logMessage));
    }

    public void SendDamageInteraction(long sessionID, int interactionIndex, int damageTaken, float durationSeconds, string endReason)
    {
        ApplyDefaultAnalyticsConfigIfMissing();

        WWWForm form = new WWWForm();
        TryAddField(form, interactionSessionIdEntry, sessionID.ToString());
        TryAddField(form, interactionIndexEntry, interactionIndex.ToString());
        TryAddField(form, interactionDamageTakenEntry, damageTaken.ToString());
        TryAddField(form, interactionDurationSecondsEntry, durationSeconds.ToString("F3"));
        TryAddField(form, interactionEndReasonEntry, endReason);

        string logMessage =
            $"Damage interaction uploaded (session {sessionID}): index={interactionIndex}, " +
            $"damage={damageTaken}, duration={durationSeconds:F3}s, endReason={endReason}";

        StartCoroutine(PostForm(form, logMessage));
    }

    private IEnumerator PostForm(WWWForm form, string successMessage)
    {
        using (UnityWebRequest www = UnityWebRequest.Post(URL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Google Form upload failed: {www.error}");
            }
            else
            {
                Debug.Log(successMessage);
            }
        }
    }

    private void TryAddField(WWWForm form, string entryId, string value)
    {
        if (string.IsNullOrWhiteSpace(entryId))
            return;

        form.AddField(entryId, value);
    }

    private int GetKillCount(Dictionary<string, int> killCounts, string key)
    {
        if (killCounts == null)
            return 0;

        return killCounts.TryGetValue(key, out int count) ? count : 0;
    }

    private void ApplyDefaultAnalyticsConfigIfMissing()
    {
        if (string.IsNullOrWhiteSpace(URL))
            URL = DefaultFormUrl;

        if (string.IsNullOrWhiteSpace(runSessionIdEntry))
            runSessionIdEntry = DefaultRunSessionIdEntry;
        if (string.IsNullOrWhiteSpace(runOutcomeEntry))
            runOutcomeEntry = DefaultRunOutcomeEntry;
        if (string.IsNullOrWhiteSpace(totalKillsEntry))
            totalKillsEntry = DefaultTotalKillsEntry;
        if (string.IsNullOrWhiteSpace(bulletKillsEntry))
            bulletKillsEntry = DefaultBulletKillsEntry;
        if (string.IsNullOrWhiteSpace(slashKillsEntry))
            slashKillsEntry = DefaultSlashKillsEntry;
        if (string.IsNullOrWhiteSpace(trapKillsEntry))
            trapKillsEntry = DefaultTrapKillsEntry;
        if (string.IsNullOrWhiteSpace(dashKillsEntry))
            dashKillsEntry = DefaultDashKillsEntry;
        if (string.IsNullOrWhiteSpace(unknownKillsEntry))
            unknownKillsEntry = DefaultUnknownKillsEntry;
        if (string.IsNullOrWhiteSpace(highestLevelReachedEntry))
            highestLevelReachedEntry = DefaultHighestLevelReachedEntry;
        if (string.IsNullOrWhiteSpace(deathLevelEntry))
            deathLevelEntry = DefaultDeathLevelEntry;

        if (string.IsNullOrWhiteSpace(ttkSessionIdEntry))
            ttkSessionIdEntry = DefaultTtkSessionIdEntry;
        if (string.IsNullOrWhiteSpace(ttkEnemyNameEntry))
            ttkEnemyNameEntry = DefaultTtkEnemyNameEntry;
        if (string.IsNullOrWhiteSpace(ttkSecondsEntry))
            ttkSecondsEntry = DefaultTtkSecondsEntry;
        if (string.IsNullOrWhiteSpace(ttkKillMethodEntry))
            ttkKillMethodEntry = DefaultTtkKillMethodEntry;
        if (string.IsNullOrWhiteSpace(ttkDamageTypesEntry))
            ttkDamageTypesEntry = DefaultTtkDamageTypesEntry;
        if (string.IsNullOrWhiteSpace(roomClearSessionIdEntry))
            roomClearSessionIdEntry = DefaultRoomClearSessionIdEntry;
        if (string.IsNullOrWhiteSpace(roomClearLevelEntry))
            roomClearLevelEntry = DefaultRoomClearLevelEntry;
        if (string.IsNullOrWhiteSpace(roomClearSecondsEntry))
            roomClearSecondsEntry = DefaultRoomClearSecondsEntry;
        if (string.IsNullOrWhiteSpace(roomClearIsBossEntry))
            roomClearIsBossEntry = DefaultRoomClearIsBossEntry;
        if (string.IsNullOrWhiteSpace(roomClearResultEntry))
            roomClearResultEntry = DefaultRoomClearResultEntry;

        if (string.IsNullOrWhiteSpace(interactionSessionIdEntry))
            interactionSessionIdEntry = DefaultInteractionSessionIdEntry;
        if (string.IsNullOrWhiteSpace(interactionIndexEntry))
            interactionIndexEntry = DefaultInteractionIndexEntry;
        if (string.IsNullOrWhiteSpace(interactionDamageTakenEntry))
            interactionDamageTakenEntry = DefaultInteractionDamageTakenEntry;
        if (string.IsNullOrWhiteSpace(interactionDurationSecondsEntry))
            interactionDurationSecondsEntry = DefaultInteractionDurationSecondsEntry;
        if (string.IsNullOrWhiteSpace(interactionEndReasonEntry))
            interactionEndReasonEntry = DefaultInteractionEndReasonEntry;
    }
}
