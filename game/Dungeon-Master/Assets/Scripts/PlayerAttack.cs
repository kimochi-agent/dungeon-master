using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem;

// Step 9: melee attack. Space / left-click / gamepad-A / right-half screen tap
// hits monsters within range. Touch attack shares the step-5 touch system.
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float range = 2.5f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float cooldown = 0.4f;

    private float cooldownTimer;

    private void Awake()
    {
        TouchJoystick.EnableTouch();
    }

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
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touch.activeTouches)
            {
                if (touch.phase == TouchPhase.Began && touch.screenPosition.x > Screen.width / 2f)
                    return true;
            }
        }
        return false;
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
