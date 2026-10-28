using System.Collections;
using UnityEngine;

// Step 8: basic monster. Wanders until the player is close, then chases and
// damages on contact. Step 9: takes damage, flashes, dies into coin drops.
[RequireComponent(typeof(CharacterController))]
public class Monster : MonoBehaviour
{
    [SerializeField] private float wanderSpeed = 1.5f;
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float chaseRange = 8f;
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int maxHealth = 30; // Step 9 (additive).

    public int Damage => damage;

    private CharacterController controller;
    private Transform player;
    private PlayerHealth playerHealth;
    private Renderer bodyRenderer;
    private Color bodyColor;
    private int health; // Step 9 (additive).
    private bool dead; // Step 9 (additive).
    private Vector3 wanderDir = Vector3.forward;
    private float wanderTimer;
    private float cooldownTimer;
    private float verticalVelocity;

    private void Awake()
    {
        LevelData cfg = GameConfig.Current; // Step 10: stats from config.
        damage = cfg.monsterDamage;
        maxHealth = cfg.monsterHealth;
        chaseSpeed = cfg.monsterSpeed;
        controller = GetComponent<CharacterController>();
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
            player = pc.transform;
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        bodyRenderer = GetComponent<Renderer>();
        if (bodyRenderer != null)
            bodyColor = bodyRenderer.material.color;
        health = maxHealth;
        PickWanderDir();
    }

    private void Update()
    {
        if (player == null || dead)
            return;

        cooldownTimer -= Time.deltaTime;
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        Vector3 move;
        if (distance < chaseRange)
        {
            move = distance > 0.01f ? toPlayer.normalized * chaseSpeed : Vector3.zero;
            Face(move);
            if (distance < attackRange && cooldownTimer <= 0f)
            {
                cooldownTimer = attackCooldown;
                playerHealth?.TakeDamage(damage);
            }
        }
        else
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
                PickWanderDir();
            move = wanderDir * wanderSpeed;
            Face(move);
        }

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += -20f * Time.deltaTime;
        controller.Move((move + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    // Step 9 (additive): damage, hit flash, death into coin drops.
    public void TakeDamage(int amount)
    {
        if (dead || amount <= 0)
            return;
        health -= amount;
        if (health <= 0)
            Die();
        else
            StartCoroutine(Flash());
    }

    private IEnumerator Flash()
    {
        if (bodyRenderer != null)
            bodyRenderer.material.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        if (bodyRenderer != null)
            bodyRenderer.material.color = bodyColor;
    }

    private void Die()
    {
        dead = true;
        GameState.AddKill(); // Step 12 (additive): feed contextual puzzles.
        for (int i = 0; i < 3; i++)
        {
            float angle = i * Mathf.PI * 2f / 3f;
            Vector3 pos = transform.position + new Vector3(Mathf.Cos(angle) * 1.2f, 0.6f, Mathf.Sin(angle) * 1.2f);
            CollectiblePlacer.CreateCoin(pos);
        }
        Debug.Log("Monster slain: 3 coins dropped.");
        Destroy(gameObject);
    }

    private void PickWanderDir()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        wanderDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        wanderTimer = Random.Range(2f, 4f);
    }

    private void Face(Vector3 move)
    {
        if (move.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(move, Vector3.up), 8f * Time.deltaTime);
    }
}
