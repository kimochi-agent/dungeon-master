using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

// Analog circular move stick, bottom-left. Drag to move (analog output).
// Attack lives on the separate AttackButton. Replaces the triangle pad
// (TouchButtons.cs, kept in the repo but unused) which had touch issues.
// Polls the touchscreen directly (no EnhancedTouch dependency); hidden
// unless a touchscreen exists. Hit zones are measured from live
// on-screen rects every frame. Publishes its zone so LookDrag ignores it.
public class MoveJoystick : MonoBehaviour
{
    public static Vector2 MoveVector { get; private set; }

    public static Vector2 ZoneCenterPx { get; private set; }
    public static float ZoneRadiusPx { get; private set; }

    private GameObject root;
    private RectTransform baseRt;
    private RectTransform knob;
    private TouchControl activeTouch;
    private Touchscreen trackedScreen;

    private void Awake()
    {
        Build();
    }

    private void OnDestroy()
    {
        MoveVector = Vector2.zero;
        ZoneCenterPx = Vector2.zero;
        ZoneRadiusPx = 0f;
        activeTouch = null;
        trackedScreen = null;
    }

    private void Update()
    {
        var screen = Touchscreen.current;
        if (screen == null)
        {
            root.SetActive(false);
            MoveVector = Vector2.zero;
            ZoneCenterPx = Vector2.zero;
            ZoneRadiusPx = 0f;
            activeTouch = null;
            trackedScreen = null;
            return;
        }
        root.SetActive(true);

        Vector2 basePx = baseRt.position;
        float baseRadiusPx = baseRt.rect.width * baseRt.lossyScale.x / 2f;
        float deflectPx = baseRadiusPx * 0.75f;
        float capturePx = baseRadiusPx * 1.2f;
        ZoneCenterPx = basePx;
        ZoneRadiusPx = capturePx;

        if (activeTouch != null && ReferenceEquals(screen, trackedScreen))
        {
            var phase = activeTouch.phase.ReadValue();
            if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                phase == UnityEngine.InputSystem.TouchPhase.Canceled ||
                phase == UnityEngine.InputSystem.TouchPhase.None ||
                phase == UnityEngine.InputSystem.TouchPhase.Began)
                Release(); // Released, or slot reused by a new touch; rescan below.
            else
            {
                Vector2 delta = activeTouch.position.ReadValue() - basePx;
                if (delta.magnitude > deflectPx)
                    delta = delta.normalized * deflectPx;
                MoveVector = delta / deflectPx;
                knob.position = basePx + delta;
            }
        }
        else
        {
            if (activeTouch != null)
                Release();
            trackedScreen = screen;
        }
        if (activeTouch == null)
        {
            foreach (var touch in screen.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began &&
                    Vector2.Distance(touch.position.ReadValue(), basePx) < capturePx)
                {
                    activeTouch = touch;
                    break;
                }
            }
        }
    }

    private void Release()
    {
        activeTouch = null;
        MoveVector = Vector2.zero;
        knob.position = baseRt.position;
    }

    private void Build()
    {
        root = new GameObject("MoveStickRoot");
        root.transform.SetParent(transform, false);

        GameObject canvasGo = new GameObject("MoveStickCanvas");
        canvasGo.transform.SetParent(root.transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GameObject baseGo = new GameObject("Base");
        baseGo.transform.SetParent(canvasGo.transform, false);
        baseRt = baseGo.AddComponent<RectTransform>();
        baseRt.anchorMin = baseRt.anchorMax = new Vector2(0f, 0f);
        baseRt.pivot = new Vector2(0.5f, 0.5f);
        baseRt.sizeDelta = new Vector2(130f, 130f);
        baseRt.anchoredPosition = new Vector2(95f, 105f);
        Image baseImage = baseGo.AddComponent<Image>();
        baseImage.sprite = CircleSprite.Get();
        baseImage.color = new Color(0.08f, 0.08f, 0.1f, 0.55f);

        GameObject knobGo = new GameObject("Knob");
        knobGo.transform.SetParent(baseGo.transform, false);
        knob = knobGo.AddComponent<RectTransform>();
        knob.anchorMin = knob.anchorMax = new Vector2(0.5f, 0.5f);
        knob.pivot = new Vector2(0.5f, 0.5f);
        knob.sizeDelta = new Vector2(60f, 60f);
        knob.anchoredPosition = Vector2.zero;
        Image knobImage = knobGo.AddComponent<Image>();
        knobImage.sprite = CircleSprite.Get();
        knobImage.color = new Color(0.85f, 0.85f, 0.88f, 0.9f);
    }
}
