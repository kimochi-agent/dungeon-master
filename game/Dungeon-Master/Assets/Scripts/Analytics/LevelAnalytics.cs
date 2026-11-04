using System;
using System.Collections.Generic;
using UnityEngine;

// Per-level analytics: attempts, deaths, kills, chests, coins, damage and
// clear times, persisted as JSON in PlayerPrefs. Later, GetPayload() feeds
// an API endpoint that generates the next level's config JSON (see
// "Future endpoint" below); only totals leave the device.
public enum LevelEndReason { Completed, Died, Abandoned }

[Serializable]
public struct LevelReport
{
    public int levelIndex;
    public string levelName;
    public int completions;
    public int attempts;
    public int deaths;
    public int kills;
    public int chestsOpened;
    public int lockedChestsOpened;
    public int lockedChestsDestroyed;
    public int gatesOpened;
    public int coinsCollected;
    public int damageTaken;
    public float bestClearTimeSeconds; // 0 = never completed.
    public float totalPlayTimeSeconds;
    public string lastPlayedUtc;
}

[Serializable]
internal struct LevelReportStore
{
    public List<LevelReport> reports;
}

public static class LevelAnalytics
{
    private const string StoreKey = "DungeonMaster_Analytics";

    private static LevelReportStore store;
    private static bool loaded;
    private static int activeLevel = -1;
    private static float attemptStart;
    private static int baseKills;
    private static int baseCoins;
    private static int baseDamage;

    public static void BeginLevel(int levelIndex, string levelName)
    {
        EnsureLoaded();
        if (activeLevel >= 0 && activeLevel != levelIndex)
            OnAttemptEnd(LevelEndReason.Abandoned);
        if (activeLevel == levelIndex)
            return;
        activeLevel = levelIndex;
        attemptStart = Time.unscaledTime;
        baseKills = GameState.Kills;
        baseCoins = GameState.Coins;
        baseDamage = GameState.DamageTaken;
        LevelReport report = ForLevel(levelIndex);
        report.levelName = levelName;
        report.attempts++;
        report.lastPlayedUtc = DateTime.UtcNow.ToString("o");
        SaveReport(report);
    }

    public static void OnAttemptEnd(LevelEndReason reason)
    {
        if (activeLevel < 0)
            return;
        EnsureLoaded();
        LevelReport report = ForLevel(activeLevel);
        report.kills += Math.Max(0, GameState.Kills - baseKills);
        report.coinsCollected += Math.Max(0, GameState.Coins - baseCoins);
        report.damageTaken += Math.Max(0, GameState.DamageTaken - baseDamage);
        float elapsed = Time.unscaledTime - attemptStart;
        report.totalPlayTimeSeconds += Math.Max(0f, elapsed);
        if (reason == LevelEndReason.Completed)
        {
            report.completions++;
            if (report.bestClearTimeSeconds <= 0f || elapsed < report.bestClearTimeSeconds)
                report.bestClearTimeSeconds = Math.Max(0f, elapsed);
        }
        else if (reason == LevelEndReason.Died)
        {
            report.deaths++;
        }
        report.lastPlayedUtc = DateTime.UtcNow.ToString("o");
        activeLevel = -1;
        SaveReport(report);
    }

    public static void RecordChestOpened(bool wasLocked)
    {
        if (activeLevel < 0)
            return;
        EnsureLoaded();
        LevelReport report = ForLevel(activeLevel);
        report.chestsOpened++;
        if (wasLocked)
            report.lockedChestsOpened++;
        SaveReport(report);
    }

    public static void RecordChestDestroyed()
    {
        if (activeLevel < 0)
            return;
        EnsureLoaded();
        LevelReport report = ForLevel(activeLevel);
        report.lockedChestsDestroyed++;
        SaveReport(report);
    }

    public static void RecordGateOpened()
    {
        if (activeLevel < 0)
            return;
        EnsureLoaded();
        LevelReport report = ForLevel(activeLevel);
        report.gatesOpened++;
        SaveReport(report);
    }

    // JSON payload for ONE level, ready to POST to the difficulty endpoint.
    public static string GetPayload(int levelIndex)
    {
        EnsureLoaded();
        return JsonUtility.ToJson(ForLevel(levelIndex));
    }

    // JSON payload for the whole campaign history.
    public static string GetAllPayloads()
    {
        EnsureLoaded();
        return JsonUtility.ToJson(store);
    }

    // ---- Generator endpoint (wired via LevelDownloader) ----
    // LevelDownloader POSTs GetPayload(clearedLevel) after each clear and
    // saves the returned config; GameConfig prefers downloads over the
    // bundled levelN.json files, which remain the offline fallback.

    private static void EnsureLoaded()
    {
        if (loaded)
            return;
        loaded = true;
        store.reports = new List<LevelReport>();
        try
        {
            string json = PlayerPrefs.GetString(StoreKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                LevelReportStore parsed = JsonUtility.FromJson<LevelReportStore>(json);
                if (parsed.reports != null)
                    store.reports = parsed.reports;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Analytics store unreadable, starting fresh: {e.Message}");
        }
    }

    private static LevelReport ForLevel(int levelIndex)
    {
        foreach (LevelReport report in store.reports)
        {
            if (report.levelIndex == levelIndex)
                return report;
        }
        return new LevelReport { levelIndex = levelIndex };
    }

    private static void SaveReport(LevelReport report)
    {
        for (int i = 0; i < store.reports.Count; i++)
        {
            if (store.reports[i].levelIndex == report.levelIndex)
            {
                store.reports[i] = report;
                Persist();
                return;
            }
        }
        store.reports.Add(report);
        Persist();
    }

    private static void Persist()
    {
        PlayerPrefs.SetString(StoreKey, JsonUtility.ToJson(store));
        PlayerPrefs.Save();
    }
}
