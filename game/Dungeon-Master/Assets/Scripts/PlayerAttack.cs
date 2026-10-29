using UnityEngine;
using UnityEngine.InputSystem;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;

// Step 9: melee attack. Space / left-click / gamepad-A / quick right-half tap
// hits monsters within range. Drags are looks (FirstPersonLook), not attacks.
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float range = 2.5f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float cooldown = 0.4f;

    private float cooldownTimer;
    private int tapTouchId = -1;
    private Vector2 tapPos;
    private float tapTime;

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
        var screen = Touchscreen.current;
        if (screen != null)
        {
            foreach (var touch in screen.touches)
            {
                int id = touch.touchId.ReadValue();
                InputTouchPhase phase = touch.phase.ReadValue();
                Vector2 pos = touch.position.ReadValue();
                if (id == tapTouchId)
                {
                    if (phase == InputTouchPhase.Ended)
                    {
                        bool quick = Time.time - tapTime < 0.35f;
                        bool still = Vector2.Distance(pos, tapPos) < 40f;
                        tapTouchId = -1;
                        if (quick && still)
                            return true;
                    }
                    else if (phase == InputTouchPhase.Canceled || phase == InputTouchPhase.None)
                    {
                        tapTouchId = -1;
                    }
                }
                else if (tapTouchId == -1 && phase == InputTouchPhase.Began && pos.x > Screen.width / 2f)
                {
                    tapTouchId = id;
                    tapPos = pos;
                    tapTime = Time.time;
                }
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
