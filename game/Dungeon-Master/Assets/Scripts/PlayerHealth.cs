using UnityEngine;
using UnityEngine.InputSystem;

// Step 4: player health, damage and death. Death ends the run and shows the
// end screen (Home / Play Again). Monsters (step 8) call TakeDamage.
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }
    public int shieldHits = 0; // Step 7 (additive): shield power-up.

    // Step 7 (additive): absorb that many hits before health drops.
    public void AddShield(int hits)
    {
        shieldHits += hits;
    }

    private void Awake()
    {
        maxHealth = GameConfig.Current.playerMaxHealth; // Step 10: config is source of truth.
        CurrentHealth = maxHealth;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
            TakeDamage(20);
#endif
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0)
            return;
        if (shieldHits > 0) // Step 7 (additive): shield absorbs the hit.
        {
            shieldHits--;
            Debug.Log($"Shield absorbed the hit. Shields left: {shieldHits}");
            return;
        }
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        GameState.AddDamageTaken(amount); // Step 12 (additive): feed contextual puzzles.
        Debug.Log($"Player took {amount} damage. Health: {CurrentHealth}/{maxHealth}");
        if (CurrentHealth <= 0)
        {
            IsDead = true;
            if (GameShell.Instance != null)
                GameShell.Instance.ShowGameOver("Game Over",
                    $"You died in the dungeon.\nCoins: {GameState.Coins}  Kills: {GameState.Kills}");
        }
    }

    public int Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return 0;
        int healed = Mathf.Min(amount, maxHealth - CurrentHealth);
        CurrentHealth += healed;
        return healed;
    }

}
