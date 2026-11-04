using System;
using UnityEngine;

// Step 10: level tuning from a JSON config file (README's `config`).
// Falls back to built-in defaults if the file is missing. One file per
// level (level1.json, level2.json, ...); layout, spawns and tuning all
// come from here so future levels (and analytics-driven configs) need
// no code changes.
[Serializable]
public struct RoomSpec
{
    public Vector2 center; // XZ center.
    public Vector2 size;   // XZ footprint.
}

[Serializable]
public struct CorridorSpec
{
    public Vector2 from; // XZ start; must terminate inside a room.
    public Vector2 to;   // XZ end; must terminate inside a room.
    public float width;  // Axis-aligned only (straight along X or Z).
}

[Serializable]
public struct DoorSpec
{
    public Vector3 position; // Must sit on a room side plane.
    public bool alongX;      // True: wall runs north-south (gap spans Z).
}

[Serializable]
public class LevelData
{
    public string levelName = "Level";
    public int playerMaxHealth = 100;
    public float playerMoveSpeed = 5f;
    public int coinCount = 10;
    public int powerUpCount = 3;
    public int chestCount = 2;
    public int lockedChestCount = 2;
    public int monsterCount = 3;
    public int monsterDamage = 10;
    public int monsterHealth = 30;
    public float monsterSpeed = 3f;
    public float puzzleTimeLimit = 10f; // Chest countdown; tune per level hardness.
    public RoomSpec[] rooms;
    public CorridorSpec[] corridors;
    public DoorSpec[] doors;
    public Vector3 playerSpawn;
    public Vector3 exitPoint;
    public Vector3[] pillars;
    public Vector3[] coinSpots;
    public Vector3[] powerUpSpots;
    public Vector3[] chestSpots;
    public Vector3[] lockedChestSpots;
    public Vector3[] monsterSpots;

    // Missing-file safety: null/empty layout arrays fall back to Level 1,
    // so the builder never sees a null. Zero spawn/exit also means missing
    // (a zero spawn would sit inside the floor; a zero exit is degenerate).
    public void FillDefaults()
    {
        rooms = OrDefault(rooms, DefaultRooms());
        corridors = OrDefault(corridors, DefaultCorridors());
        doors = OrDefault(doors, DefaultDoors());
        pillars = OrDefault(pillars, DefaultPillars());
        coinSpots = OrDefault(coinSpots, DefaultCoinSpots());
        powerUpSpots = OrDefault(powerUpSpots, DefaultPowerUpSpots());
        chestSpots = OrDefault(chestSpots, DefaultChestSpots());
        lockedChestSpots = OrDefault(lockedChestSpots, DefaultLockedChestSpots());
        monsterSpots = OrDefault(monsterSpots, DefaultMonsterSpots());
        if (playerSpawn == Vector3.zero)
            playerSpawn = new Vector3(-6f, 1f, 0f);
        if (exitPoint == Vector3.zero)
            exitPoint = new Vector3(46f, 1f, 0f);
    }

    private static T[] OrDefault<T>(T[] array, T[] fallback)
    {
        return array == null || array.Length == 0 ? fallback : array;
    }

    private static RoomSpec[] DefaultRooms()
    {
        return new RoomSpec[]
        {
            new RoomSpec { center = new Vector2(0f, 0f), size = new Vector2(20f, 20f) },
            new RoomSpec { center = new Vector2(25f, 0f), size = new Vector2(14f, 14f) },
            new RoomSpec { center = new Vector2(46f, 0f), size = new Vector2(12f, 12f) },
        };
    }

    private static CorridorSpec[] DefaultCorridors()
    {
        return new CorridorSpec[]
        {
            new CorridorSpec { from = new Vector2(10f, 0f), to = new Vector2(18f, 0f), width = 4f },
            new CorridorSpec { from = new Vector2(32f, 0f), to = new Vector2(40f, 0f), width = 4f },
        };
    }

    private static DoorSpec[] DefaultDoors()
    {
        return new DoorSpec[]
        {
            new DoorSpec { position = new Vector3(18f, 1.5f, 0f), alongX = true },
            new DoorSpec { position = new Vector3(32f, 1.5f, 0f), alongX = true },
        };
    }

    private static Vector3[] DefaultPillars()
    {
        return new Vector3[]
        {
            new Vector3(-5f, 0f, -5f),
            new Vector3(-5f, 0f, 5f),
            new Vector3(25f, 0f, 3f),
        };
    }

    private static Vector3[] DefaultCoinSpots()
    {
        return new Vector3[]
        {
            new Vector3(-3f, 0.6f, -3f),
            new Vector3(0f, 0.6f, 6f),
            new Vector3(3f, 0.6f, 0f),
            new Vector3(6f, 0.6f, -4f),
            new Vector3(-6f, 0.6f, 3f),
            new Vector3(12f, 0.6f, 0f),
            new Vector3(16f, 0.6f, 0f),
            new Vector3(20f, 0.6f, -4f),
            new Vector3(28f, 0.6f, 4f),
            new Vector3(30f, 0.6f, -4f),
        };
    }

    private static Vector3[] DefaultPowerUpSpots()
    {
        return new Vector3[]
        {
            new Vector3(6f, 0.6f, 6f),
            new Vector3(22f, 0.6f, 4f),
            new Vector3(29f, 0.6f, 0f),
        };
    }

    private static Vector3[] DefaultChestSpots()
    {
        return new Vector3[]
        {
            new Vector3(-8f, 0f, 8f),
            new Vector3(30f, 0f, 5f),
        };
    }

    private static Vector3[] DefaultLockedChestSpots()
    {
        return new Vector3[]
        {
            new Vector3(8f, 0f, -8f),
            new Vector3(27f, 0f, -5f),
        };
    }

    private static Vector3[] DefaultMonsterSpots()
    {
        return new Vector3[]
        {
            new Vector3(-2f, 1f, -7f),
            new Vector3(7f, 1f, 2f),
            new Vector3(24f, 1f, -4f),
        };
    }
}

public enum LevelSource { Bundled, Downloaded }

public static class GameConfig
{
    public static int LevelIndex { get; private set; } = 1;

    // Where the current level came from; logged on every load.
    public static LevelSource Source { get; private set; } = LevelSource.Bundled;

    private static LevelData current;
    private static int levelCount = -1; // Cached campaign length.

    public static LevelData Current
    {
        get
        {
            if (current == null)
                current = Load(LevelIndex);
            return current;
        }
    }

    public static void LoadLevel(int index)
    {
        LevelIndex = Mathf.Max(1, index);
        current = null;
    }

    public static bool HasLevel(int index)
    {
        if (index < 1)
            return false;
        LevelData downloaded;
        string downloadError;
        if (LevelDownloader.CheckDownloaded(index, out downloaded, out downloadError)
            == LevelDownloader.DownloadStatus.Valid)
            return true;
        return HasBundled(index);
    }

    private static bool HasBundled(int index)
    {
        return index >= 1 && Resources.Load<TextAsset>($"Config/level{index}") != null;
    }

    public static int LevelCount
    {
        get
        {
            if (levelCount < 0)
            {
                int bundled = 0;
                while (HasBundled(bundled + 1))
                    bundled++;
                int downloaded = 0;
                LevelData data;
                string error;
                while (LevelDownloader.CheckDownloaded(downloaded + 1, out data, out error)
                    == LevelDownloader.DownloadStatus.Valid)
                    downloaded++;
                levelCount = Mathf.Max(1, Mathf.Max(bundled, downloaded));
            }
            return levelCount;
        }
    }

    // Downloads land after this may have been cached; call after each save.
    public static void RefreshLevels()
    {
        levelCount = -1;
    }

    private static LevelData Load(int index)
    {
        LevelData downloaded;
        string downloadError;
        LevelDownloader.DownloadStatus status =
            LevelDownloader.CheckDownloaded(index, out downloaded, out downloadError);
        if (status == LevelDownloader.DownloadStatus.Valid)
        {
            Source = LevelSource.Downloaded;
            Debug.Log($"GameConfig: level {index} loaded from download.");
            return downloaded;
        }
        if (status == LevelDownloader.DownloadStatus.Invalid)
        {
            Debug.LogWarning($"GameConfig: deleting invalid level {index} download ({downloadError}).");
            LevelDownloader.DeleteDownload(index);
        }
        try
        {
            TextAsset asset = Resources.Load<TextAsset>($"Config/level{index}");
            if (asset != null)
            {
                LevelData data = JsonUtility.FromJson<LevelData>(asset.text);
                data.FillDefaults();
                Source = LevelSource.Bundled;
                Debug.Log($"GameConfig: level {index} loaded from bundled config.");
                return data;
            }
            Debug.LogWarning($"Level config {index} missing, using defaults.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Level config not loaded, using defaults: {e.Message}");
        }
        Source = LevelSource.Bundled;
        LevelData fallback = new LevelData();
        fallback.FillDefaults();
        return fallback;
    }
}
