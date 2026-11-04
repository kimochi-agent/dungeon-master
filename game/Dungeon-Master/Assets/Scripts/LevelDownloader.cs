using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Downloads generated levels from the level_generator service
// (see level_generator/contract.md). POSTs the cleared level's analytics
// on level-complete, saves the next config under persistentDataPath, and
// GameConfig prefers it over bundled files. Any failure silently falls
// back to bundled levels. Survives scene loads so a fetch started on the
// level-complete screen can land while the player is browsing Home.
public class LevelDownloader : MonoBehaviour
{
    public const string DefaultBaseUrl = "http://127.0.0.1:8124";
    public const int SchemaVersion = 1;
    public const int TimeoutSeconds = 5;
    public const int MaxAttempts = 3;
    public const int MaxResponseBytes = 65536;

    public enum FetchState { Idle, Fetching, Ready, UsingBundled }
    public enum DownloadStatus { Missing, Valid, Invalid }

    public static LevelDownloader Instance { get; private set; }
    public static FetchState State { get; private set; } = FetchState.Idle;
    public static string LastError { get; private set; } = "";
    public static event Action StateChanged;

    [Serializable]
    private class LevelEnvelope
    {
        public int levelIndex;
        public long seed;
        public string generatorVersion;
        public string layoutId;
        public string layoutHash;
        public string difficulty;
        public LevelData config;
    }

    [Serializable]
    private class LevelMeta
    {
        public int levelIndex;
        public long seed;
        public string generatorVersion;
        public string layoutId;
        public string layoutHash;
        public string difficulty;
    }

    private int fetchingNext = -1;

    public static string LevelsDir => Path.Combine(Application.persistentDataPath, "levels");

    public static string DownloadPath(int index)
    {
        return Path.Combine(LevelsDir, "level" + index + ".json");
    }

    private static string MetaPath(int index)
    {
        return Path.Combine(LevelsDir, "level" + index + ".meta.json");
    }

    // Dev override without rebuild: adb-push a URL into this file.
    public static string BaseUrl
    {
        get
        {
            try
            {
                string overridePath = Path.Combine(Application.persistentDataPath, "api_base_url.txt");
                if (File.Exists(overridePath))
                {
                    string custom = File.ReadAllText(overridePath).Trim();
                    if (!string.IsNullOrEmpty(custom))
                        return custom.TrimEnd('/');
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"LevelDownloader: base URL override unreadable: {e.Message}");
            }
            return DefaultBaseUrl;
        }
    }

    public static void EnsureExists()
    {
        if (Instance == null)
            new GameObject("LevelDownloader").AddComponent<LevelDownloader>();
    }

    public static void FetchNext(int clearedIndex)
    {
        EnsureExists();
        Instance.StartFetch(clearedIndex);
    }

    public static DownloadStatus CheckDownloaded(int index, out LevelData data, out string error)
    {
        data = null;
        error = null;
        string path = DownloadPath(index);
        if (!File.Exists(path))
            return DownloadStatus.Missing;
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception e)
        {
            error = "unreadable download: " + e.Message;
            return DownloadStatus.Invalid;
        }
        if (text.Length > MaxResponseBytes)
        {
            error = "download too large";
            return DownloadStatus.Invalid;
        }
        if (!LevelValidator.TryParse(text, out data, out error))
            return DownloadStatus.Invalid;
        return DownloadStatus.Valid;
    }

    public static void DeleteDownload(int index)
    {
        try
        {
            if (File.Exists(DownloadPath(index)))
                File.Delete(DownloadPath(index));
            if (File.Exists(MetaPath(index)))
                File.Delete(MetaPath(index));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"LevelDownloader: could not delete level {index} download: {e.Message}");
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void StartFetch(int clearedIndex)
    {
        int nextIndex = clearedIndex + 1;
        if (fetchingNext == nextIndex)
            return;
        LevelData existing;
        string existingError;
        DownloadStatus status = CheckDownloaded(nextIndex, out existing, out existingError);
        if (status == DownloadStatus.Valid)
        {
            SetState(FetchState.Ready, "");
            return;
        }
        if (status == DownloadStatus.Invalid)
            DeleteDownload(nextIndex);
        string payload = LevelAnalytics.GetPayload(clearedIndex);
        if (string.IsNullOrEmpty(payload))
        {
            SetState(FetchState.UsingBundled, "no analytics payload");
            return;
        }
        fetchingNext = nextIndex;
        SetState(FetchState.Fetching, "");
        StartCoroutine(FetchRoutine(clearedIndex, nextIndex, payload));
    }

    private static void SetState(FetchState state, string error)
    {
        State = state;
        LastError = error ?? "";
        StateChanged?.Invoke();
    }

    private IEnumerator FetchRoutine(int clearedIndex, int nextIndex, string payload)
    {
        string body = "{\"schemaVersion\":" + SchemaVersion
            + ",\"clearedLevelIndex\":" + clearedIndex
            + ",\"report\":" + payload + "}";
        byte[] bytes = Encoding.UTF8.GetBytes(body);
        string url = BaseUrl + "/api/levels/next";
        string lastError = "";
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(bytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = TimeoutSeconds;
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success && request.responseCode == 200)
                {
                    byte[] data = request.downloadHandler.data;
                    if (data != null && data.Length <= MaxResponseBytes)
                    {
                        string saveError;
                        if (TrySaveResponse(Encoding.UTF8.GetString(data), nextIndex, out saveError))
                        {
                            fetchingNext = -1;
                            GameConfig.RefreshLevels();
                            SetState(FetchState.Ready, "");
                            yield break;
                        }
                        lastError = saveError;
                    }
                    else
                    {
                        lastError = "response too large";
                    }
                }
                else
                {
                    lastError = "attempt " + attempt + ": " + request.error
                        + " (HTTP " + request.responseCode + ")";
                }
            }
            if (attempt < MaxAttempts)
                yield return new WaitForSecondsRealtime(1f);
        }
        fetchingNext = -1;
        Debug.LogWarning($"LevelDownloader: level {nextIndex} download failed ({lastError}); using bundled.");
        SetState(FetchState.UsingBundled, lastError);
    }

    private bool TrySaveResponse(string response, int nextIndex, out string error)
    {
        error = null;
        LevelEnvelope envelope;
        try
        {
            envelope = JsonUtility.FromJson<LevelEnvelope>(response);
        }
        catch (Exception e)
        {
            error = "bad envelope: " + e.Message;
            return false;
        }
        if (envelope == null || envelope.config == null)
        {
            error = "response has no level config";
            return false;
        }
        if (envelope.levelIndex != nextIndex)
        {
            error = $"response is for level {envelope.levelIndex}, want {nextIndex}";
            return false;
        }
        string configError;
        if (!LevelValidator.Validate(envelope.config, out configError))
        {
            error = "invalid level config: " + configError;
            return false;
        }
        try
        {
            Directory.CreateDirectory(LevelsDir);
            File.WriteAllText(DownloadPath(nextIndex), JsonUtility.ToJson(envelope.config));
            LevelMeta meta = new LevelMeta
            {
                levelIndex = envelope.levelIndex,
                seed = envelope.seed,
                generatorVersion = envelope.generatorVersion,
                layoutId = envelope.layoutId,
                layoutHash = envelope.layoutHash,
                difficulty = envelope.difficulty,
            };
            File.WriteAllText(MetaPath(nextIndex), JsonUtility.ToJson(meta));
        }
        catch (Exception e)
        {
            error = "could not save download: " + e.Message;
            return false;
        }
        LevelData verify;
        string verifyError;
        if (CheckDownloaded(nextIndex, out verify, out verifyError) != DownloadStatus.Valid)
        {
            error = "saved download failed to reload: " + verifyError;
            DeleteDownload(nextIndex);
            return false;
        }
        Debug.Log($"LevelDownloader: level {nextIndex} downloaded "
            + $"(layout {envelope.layoutId}, {envelope.difficulty}, hash {envelope.layoutHash}).");
        return true;
    }
}
