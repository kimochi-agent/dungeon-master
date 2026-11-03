using System;
using System.Collections.Generic;
using UnityEngine;

// Shared run state: collected coins and power-ups. HUD (step 4) will read this.
// Step 12: also tracks kills and damage taken to feed contextual puzzles.
// TotalCoins is the persisted lifetime count shown on the home scene.
public static class GameState
{
    public static int Coins;
    public static readonly List<string> PowerUps = new List<string>();

    private const string TotalCoinsKey = "DungeonMaster_TotalCoins";
    public static int TotalCoins { get; private set; }

    private const string NextLevelKey = "DungeonMaster_NextLevel";
    public static int NextLevel { get; private set; } // Persisted: the level Home offers.

    static GameState()
    {
        TotalCoins = PlayerPrefs.GetInt(TotalCoinsKey, 0);
        NextLevel = Mathf.Max(1, PlayerPrefs.GetInt(NextLevelKey, 1));
    }

    public static int Kills;
    public static int DamageTaken;
    public static int ChestsOpened;

    public static event Action Changed;

    public static void AddCoins(int amount)
    {
        Coins += amount;
        TotalCoins += amount; // Banked immediately; Reset() only clears the run count.
        PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static void AddPowerUp(string kind)
    {
        PowerUps.Add(kind);
        Changed?.Invoke();
    }

    public static void AddKill()
    {
        Kills++;
        Changed?.Invoke();
    }

    public static void AddChestOpened()
    {
        ChestsOpened++;
        Changed?.Invoke();
    }

    public static void AddDamageTaken(int amount)
    {
        DamageTaken += amount;
        Changed?.Invoke();
    }

    public static void UnlockLevel(int level)
    {
        if (level > NextLevel)
        {
            NextLevel = level;
            PlayerPrefs.SetInt(NextLevelKey, NextLevel);
            PlayerPrefs.Save();
        }
    }

    public static void Reset()
    {
        Coins = 0;
        PowerUps.Clear();
        Kills = 0;
        DamageTaken = 0;
        ChestsOpened = 0;
        Changed?.Invoke();
    }
}
