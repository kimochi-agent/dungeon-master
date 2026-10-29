using UnityEngine;
using UnityEngine.InputSystem;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;

// First-person look: mouse movement turns the view on desktop (while the
// pointer is locked); right-half touch drag turns it on mobile. Yaw rotates
// the player body, pitch rotates the head camera. Click re-locks on desktop.
[RequireComponent(typeof(PlayerController))]
public class FirstPersonLook : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float touchSensitivity = 0.3f;

    private Camera headCamera;
    private float pitch;
    private int lookTouchId = -1;
    private Vector2 lastTouchPos;

    private void Awake()
    {
        headCamera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        if (headCamera == null)
            headCamera = GetComponentInChildren<Camera>();

        var mouse = Mouse.current;
        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 delta = mouse.delta.ReadValue();
            ApplyLook(delta.x * mouseSensitivity, -delta.y * mouseSensitivity);
        }
        if (mouse != null && Cursor.lockState == CursorLockMode.None &&
            GameShell.Instance != null && GameShell.Instance.IsPlaying &&
            mouse.leftButton.wasPressedThisFrame)
            Cursor.lockState = CursorLockMode.Locked;

        var screen = Touchscreen.current;
        if (screen == null)
            return;
        bool found = false;
        foreach (var touch in screen.touches)
        {
            int id = touch.touchId.ReadValue();
            InputTouchPhase phase = touch.phase.ReadValue();
            Vector2 pos = touch.position.ReadValue();
            if (id == lookTouchId)
            {
                found = true;
                if (phase == InputTouchPhase.Ended || phase == InputTouchPhase.Canceled ||
                    phase == InputTouchPhase.None)
                {
                    lookTouchId = -1;
                }
                else if (phase == InputTouchPhase.Moved)
                {
                    Vector2 drag = pos - lastTouchPos;
                    ApplyLook(drag.x * touchSensitivity, -drag.y * touchSensitivity);
                    lastTouchPos = pos;
                }
            }
            else if (lookTouchId == -1 && phase == InputTouchPhase.Began && pos.x > Screen.width / 2f)
            {
                lookTouchId = id;
                lastTouchPos = pos;
                found = true;
            }
        }
        if (lookTouchId != -1 && !found)
            lookTouchId = -1;
    }

    private void ApplyLook(float yawDelta, float pitchDelta)
    {
        transform.Rotate(0f, yawDelta, 0f);
        pitch = Mathf.Clamp(pitch + pitchDelta, -85f, 85f);
        if (headCamera != null)
            headCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
