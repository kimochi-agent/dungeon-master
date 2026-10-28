using System;
using System.Collections.Generic;

// Shared run state: collected coins and power-ups. HUD (step 4) will read this.
// Step 12: also tracks kills and damage taken to feed contextual puzzles.
public static class GameState
{
    public static int Coins;
    public static readonly List<string> PowerUps = new List<string>();
    public static int Kills;
    public static int DamageTaken;

    public static event Action Changed;

    public static void AddCoins(int amount)
    {
        Coins += amount;
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

    public static void AddDamageTaken(int amount)
    {
        DamageTaken += amount;
        Changed?.Invoke();
    }

    public static void Reset()
    {
        Coins = 0;
        PowerUps.Clear();
        Kills = 0;
        DamageTaken = 0;
        Changed?.Invoke();
    }
}
