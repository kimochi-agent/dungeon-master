using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Invisible full-screen drag look (touch only): dragging anywhere turns the
// camera, replacing the circular look stick (TouchJoystick.cs, kept unused).
// Touches that begin on the move stick or attack button are ignored here so
// each finger keeps one job. No look while paused, on end screens, or while
// a puzzle popup is open. Deltas accumulate until FirstPersonLook spends
// them each frame.
public class LookDrag : MonoBehaviour
{
    private static Vector2 pendingPx;

    public static Vector2 ConsumeDelta()
    {
        Vector2 d = pendingPx;
        pendingPx = Vector2.zero;
        return d;
    }

    private TouchControl activeTouch;
    private Touchscreen trackedScreen;

    private void OnDestroy()
    {
        pendingPx = Vector2.zero;
        activeTouch = null;
        trackedScreen = null;
    }

    private void Update()
    {
        var screen = Touchscreen.current;
        if (screen == null || !CanLook())
        {
            activeTouch = null;
            pendingPx = Vector2.zero;
            return;
        }
        if (activeTouch != null && ReferenceEquals(screen, trackedScreen))
        {
            var phase = activeTouch.phase.ReadValue();
            if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                phase == UnityEngine.InputSystem.TouchPhase.Canceled ||
                phase == UnityEngine.InputSystem.TouchPhase.None ||
                phase == UnityEngine.InputSystem.TouchPhase.Began)
                activeTouch = null; // Released, or slot reused by a new touch; rescan below.
            else
            {
                pendingPx += activeTouch.delta.ReadValue();
                return;
            }
        }
        else
        {
            activeTouch = null;
            trackedScreen = screen;
        }
        foreach (var touch in screen.touches)
        {
            if (touch.phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.Began)
                continue;
            Vector2 pos = touch.position.ReadValue();
            if (!InZone(pos, MoveJoystick.ZoneCenterPx, MoveJoystick.ZoneRadiusPx) &&
                !InZone(pos, AttackButton.ZoneCenterPx, AttackButton.ZoneRadiusPx))
            {
                activeTouch = touch;
                break;
            }
        }
    }

    private static bool InZone(Vector2 pos, Vector2 center, float radius)
    {
        return radius > 0f && Vector2.Distance(pos, center) < radius;
    }

    private static bool CanLook()
    {
        return (GameShell.Instance == null || GameShell.Instance.IsPlaying) && !PuzzleUI.IsOpen;
    }
}
