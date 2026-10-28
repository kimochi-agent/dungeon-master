using System;
using System.Collections.Generic;

// Shared run state: collected coins and power-ups. HUD (step 4) will read this.
public static class GameState
{
    public static int Coins;
    public static readonly List<string> PowerUps = new List<string>();

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

    public static void Reset()
    {
        Coins = 0;
        PowerUps.Clear();
        Changed?.Invoke();
    }
}
