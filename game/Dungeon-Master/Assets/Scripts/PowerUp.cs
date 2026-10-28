using UnityEngine;

// Step 2: power-up pickup. Stored in inventory; effects arrive in step 7.
public enum PowerUpKind { Speed, Health, Shield }

public class PowerUp : Collectible
{
    public PowerUpKind kind = PowerUpKind.Speed;

    protected override void Collect()
    {
        GameState.AddPowerUp(kind.ToString());
        Debug.Log($"Power-up collected: {kind} (effects arrive in step 7).");
    }
}
