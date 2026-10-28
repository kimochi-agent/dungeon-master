using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Step 5: fixed virtual joystick, bottom-left. Touch only: hidden unless a
// touchscreen exists. PlayerController adds Vector to keyboard/gamepad input.
public class TouchJoystick : MonoBehaviour
{
    public static Vector2 Vector { get; private set; }

    private static bool touchEnabled;

    public static void EnableTouch()
    {
        if (touchEnabled)
            return;
        touchEnabled = true;
        EnhancedTouchSupport.Enable();
    }

    [SerializeField] private float radius = 90f;

    private GameObject root;
    private Canvas canvas;
    private RectTransform knob;
    private Vector2 baseScreenPos;
    private int activeTouchId = -1;

    private void Awake()
    {
        EnableTouch();
        Build();
    }

    private void OnDestroy()
    {
        Vector = Vector2.zero;
        activeTouchId = -1;
    }

    private void Update()
    {
        if (Touchscreen.current == null)
        {
            root.SetActive(false);
            Vector = Vector2.zero;
            activeTouchId = -1;
            return;
        }
        root.SetActive(true);

        bool found = false;
        foreach (var touch in Touch.activeTouches)
        {
            if (touch.touchId == activeTouchId)
            {
                found = true;
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    Release();
                }
                else
                {
                    Vector2 delta = touch.screenPosition - baseScreenPos;
                    if (delta.magnitude > radius)
                        delta = delta.normalized * radius;
                    Vector = delta / radius;
                    knob.anchoredPosition = delta / canvas.scaleFactor;
                }
            }
            else if (activeTouchId == -1 && touch.phase == TouchPhase.Began &&
                     Vector2.Distance(touch.screenPosition, baseScreenPos) < radius * 1.6f)
            {
                activeTouchId = touch.touchId;
                found = true;
            }
        }
        if (activeTouchId != -1 && !found)
            Release();
    }

    private void Release()
    {
        activeTouchId = -1;
        Vector = Vector2.zero;
        knob.anchoredPosition = Vector2.zero;
    }

    private void Build()
    {
        root = new GameObject("JoystickRoot");
        root.transform.SetParent(transform, false);

        GameObject canvasGo = new GameObject("JoystickCanvas");
        canvasGo.transform.SetParent(root.transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GameObject baseGo = new GameObject("Base");
        baseGo.transform.SetParent(canvasGo.transform, false);
        RectTransform baseRt = baseGo.AddComponent<RectTransform>();
        baseRt.anchorMin = baseRt.anchorMax = new Vector2(0f, 0f);
        baseRt.pivot = new Vector2(0.5f, 0.5f);
        baseRt.sizeDelta = new Vector2(240f, 240f);
        baseRt.anchoredPosition = new Vector2(150f, 170f);
        baseGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.25f);

        GameObject knobGo = new GameObject("Knob");
        knobGo.transform.SetParent(baseGo.transform, false);
        knob = knobGo.AddComponent<RectTransform>();
        knob.anchorMin = knob.anchorMax = new Vector2(0.5f, 0.5f);
        knob.pivot = new Vector2(0.5f, 0.5f);
        knob.sizeDelta = new Vector2(110f, 110f);
        knob.anchoredPosition = Vector2.zero;
        knobGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.6f);

        // Base center in screen pixels (matches touch coordinates).
        Vector2 baseCanvas = new Vector2(150f, 170f);
        baseScreenPos = new Vector2(baseCanvas.x * canvas.scaleFactor,
            baseCanvas.y * canvas.scaleFactor);
    }
}
