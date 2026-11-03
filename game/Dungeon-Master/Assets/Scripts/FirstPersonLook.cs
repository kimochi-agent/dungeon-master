using UnityEngine;
using UnityEngine.InputSystem;

// First-person look: mouse movement turns the view on desktop (while the
// pointer is locked); dragging anywhere on the touchscreen turns it on
// mobile (except over the move stick and attack button). Yaw rotates
// the player body, pitch rotates the head camera. Click re-locks on desktop.
[RequireComponent(typeof(PlayerController))]
public class FirstPersonLook : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 0.02f;
    [SerializeField] private float dragSensitivity = 0.25f;

    private Camera headCamera;
    private float pitch;

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

        Vector2 dragPx = LookDrag.ConsumeDelta();
        if (dragPx.sqrMagnitude > 0.0001f)
            ApplyLook(dragPx.x * dragSensitivity, -dragPx.y * dragSensitivity);
    }

    private void ApplyLook(float yawDelta, float pitchDelta)
    {
        transform.Rotate(0f, yawDelta, 0f);
        pitch = Mathf.Clamp(pitch + pitchDelta, -85f, 85f);
        if (headCamera != null)
            headCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
