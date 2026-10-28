using UnityEngine;

// Step 2: power-up pickup. Step 7: applies its effect on collect.
public enum PowerUpKind { Speed, Health, Shield }

public class PowerUp : Collectible
{
    public PowerUpKind kind = PowerUpKind.Speed;

    protected override void Collect()
    {
        GameState.AddPowerUp(kind.ToString());
        ApplyEffect();
        Debug.Log($"Power-up collected: {kind}.");
    }

    private void ApplyEffect()
    {
        switch (kind)
        {
            case PowerUpKind.Speed:
                var controller = FindFirstObjectByType<PlayerController>();
                if (controller != null)
                    controller.ApplySpeedBoost(1.5f, 10f);
                break;
            case PowerUpKind.Health:
                var health = FindFirstObjectByType<PlayerHealth>();
                if (health != null)
                    health.Heal(50);
                break;
            case PowerUpKind.Shield:
                var shielded = FindFirstObjectByType<PlayerHealth>();
                if (shielded != null)
                    shielded.AddShield(3);
                break;
        }
    }
}
