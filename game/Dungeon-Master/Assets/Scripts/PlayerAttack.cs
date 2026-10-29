using UnityEngine;
using UnityEngine.InputSystem;

// Step 9: melee attack. Space / left-click / gamepad-A / move-stick tap
// hits monsters within range.
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float range = 2.5f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float cooldown = 0.4f;

    private float cooldownTimer;

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f)
            return;
        if (AttackPressed())
        {
            cooldownTimer = cooldown;
            Attack();
        }
    }

    private bool AttackPressed()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            return true;
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            return true;
        var gamepad = Gamepad.current;
        if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
            return true;
        return MoveJoystick.ConsumeAttack();
    }

    private void Attack()
    {
        Debug.Log("Player attacks.");
        foreach (var monster in FindObjectsByType<Monster>(FindObjectsSortMode.None))
        {
            if (Vector3.Distance(transform.position, monster.transform.position) <= range)
                monster.TakeDamage(damage);
        }
    }
}
