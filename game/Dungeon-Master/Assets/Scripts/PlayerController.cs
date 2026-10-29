using UnityEngine;
using UnityEngine.InputSystem;

// Step 1: moves the player across the dungeon floor (WASD/arrows or gamepad).
// First-person rework: movement is body-relative (input follows the look yaw).
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;

    public float speedMultiplier = 1f; // Step 7 (additive): speed power-up.

    private CharacterController controller;
    private float verticalVelocity;
    private Coroutine speedBoostRoutine;

    // Step 7 (additive): temporary speed boost. Re-pickup restarts the timer.
    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (speedBoostRoutine != null)
            StopCoroutine(speedBoostRoutine);
        speedBoostRoutine = StartCoroutine(SpeedBoost(duration, multiplier));
    }

    private System.Collections.IEnumerator SpeedBoost(float duration, float multiplier)
    {
        speedMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        speedMultiplier = 1f;
        speedBoostRoutine = null;
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        moveSpeed = GameConfig.Current.playerMoveSpeed; // Step 10: config is source of truth.
    }

    private void Update()
    {
        Vector2 input = ReadMoveInput();
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();
        Vector3 move = right * input.x + forward * input.y;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        controller.Move((move * moveSpeed * speedMultiplier + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    private static Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                input.y -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                input.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                input.x += 1f;
        }

        var gamepad = Gamepad.current;
        if (gamepad != null)
            input += gamepad.leftStick.ReadValue();

        input += TouchJoystick.Vector; // Step 5 (additive): touch joystick.

        return input;
    }
}
