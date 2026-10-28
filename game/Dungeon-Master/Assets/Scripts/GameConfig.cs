using System;
using UnityEngine;

// Step 10: level tuning from a JSON config file (README's `config`).
// Falls back to built-in defaults if the file is missing.
[Serializable]
public class LevelData
{
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
}

public static class GameConfig
{
    private static LevelData current;

    public static LevelData Current
    {
        get
        {
            if (current == null)
                current = Load();
            return current;
        }
    }

    private static LevelData Load()
    {
        try
        {
            TextAsset asset = Resources.Load<TextAsset>("Config/level1");
            if (asset != null)
                return JsonUtility.FromJson<LevelData>(asset.text);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Level config not loaded, using defaults: {e.Message}");
        }
        return new LevelData();
    }
}
