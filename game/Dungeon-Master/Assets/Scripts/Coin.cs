using UnityEngine;

// Step 2: coin pickup. Adds to the run coin count.
public class Coin : Collectible
{
    [SerializeField] private int value = 1;

    protected override void Collect()
    {
        GameState.AddCoins(value);
        Debug.Log($"Coin collected. Total: {GameState.Coins}");
    }
}
