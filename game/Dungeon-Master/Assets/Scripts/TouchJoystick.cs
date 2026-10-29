using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Step 5: fixed circular look stick, bottom-left. Polls the touchscreen
// control directly (no EnhancedTouch dependency). Hidden unless a touchscreen
// exists. Hit zones are measured from live on-screen rects every frame, so
// they always match the visuals. Drives the camera (look).
public class TouchJoystick : MonoBehaviour
{
    public static Vector2 Vector { get; private set; }

    private GameObject root;
    private RectTransform baseRt;
    private RectTransform knob;
    private int activeTouchId = -1;

    private void Awake()
    {
        Build();
    }

    private void OnDestroy()
    {
        Vector = Vector2.zero;
        activeTouchId = -1;
    }

    private void Update()
    {
        var screen = Touchscreen.current;
        if (screen == null)
        {
            root.SetActive(false);
            Vector = Vector2.zero;
            activeTouchId = -1;
            return;
        }
        root.SetActive(true);

        Vector2 basePx = baseRt.position;
        float baseRadiusPx = baseRt.rect.width * baseRt.lossyScale.x / 2f;
        float deflectPx = baseRadiusPx * 0.75f;
        float capturePx = baseRadiusPx * 1.2f;

        bool found = false;
        foreach (var touch in screen.touches)
        {
            int id = touch.touchId.ReadValue();
            var phase = touch.phase.ReadValue();
            if (id == activeTouchId)
            {
                found = true;
                if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                    phase == UnityEngine.InputSystem.TouchPhase.Canceled ||
                    phase == UnityEngine.InputSystem.TouchPhase.None)
                {
                    Release();
                }
                else
                {
                    Vector2 delta = touch.position.ReadValue() - basePx;
                    if (delta.magnitude > deflectPx)
                        delta = delta.normalized * deflectPx;
                    Vector = delta / deflectPx;
                    knob.position = basePx + delta;
                }
            }
            else if (activeTouchId == -1 && phase == UnityEngine.InputSystem.TouchPhase.Began &&
                     Vector2.Distance(touch.position.ReadValue(), basePx) < capturePx)
            {
                activeTouchId = id;
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
        knob.position = baseRt.position;
    }

    private void Build()
    {
        root = new GameObject("JoystickRoot");
        root.transform.SetParent(transform, false);

        GameObject canvasGo = new GameObject("JoystickCanvas");
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
