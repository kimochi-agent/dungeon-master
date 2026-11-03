using UnityEngine;
using UnityEngine.InputSystem;

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

    private int activeTouchId = -1;

    private void OnDestroy()
    {
        pendingPx = Vector2.zero;
        activeTouchId = -1;
    }

    private void Update()
    {
        var screen = Touchscreen.current;
        if (screen == null || !CanLook())
        {
            activeTouchId = -1;
            pendingPx = Vector2.zero;
            return;
        }
        foreach (var touch in screen.touches)
        {
            int id = touch.touchId.ReadValue();
            var phase = touch.phase.ReadValue();
            if (id == activeTouchId)
            {
                if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                    phase == UnityEngine.InputSystem.TouchPhase.Canceled ||
                    phase == UnityEngine.InputSystem.TouchPhase.None)
                    activeTouchId = -1;
                else
                    pendingPx += touch.delta.ReadValue();
            }
            else if (activeTouchId == -1 && phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                Vector2 pos = touch.position.ReadValue();
                if (!InZone(pos, MoveJoystick.ZoneCenterPx, MoveJoystick.ZoneRadiusPx) &&
                    !InZone(pos, AttackButton.ZoneCenterPx, AttackButton.ZoneRadiusPx))
                    activeTouchId = id;
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
