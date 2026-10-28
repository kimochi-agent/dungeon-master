using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Step 4: player health, damage, death and respawn.
// Monsters (step 8) will call TakeDamage; H key is a dev-only stand-in.
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    private void Awake()
    {
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
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        Debug.Log($"Player took {amount} damage. Health: {CurrentHealth}/{maxHealth}");
        if (CurrentHealth <= 0)
            StartCoroutine(DieAndRespawn());
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
    }

    private IEnumerator DieAndRespawn()
    {
        IsDead = true;
        Debug.Log("Player died. Respawning...");
        yield return new WaitForSeconds(1f);
        var dungeon = FindFirstObjectByType<DungeonGenerator>();
        var controller = GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;
        transform.position = dungeon != null ? dungeon.SpawnPoint : Vector3.up;
        transform.rotation = Quaternion.identity;
        if (controller != null)
            controller.enabled = true;
        CurrentHealth = maxHealth;
        IsDead = false;
        Debug.Log("Player respawned with full health.");
    }
}
