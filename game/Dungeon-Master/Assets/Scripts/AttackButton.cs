using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

// Circular attack button, bottom-right (where the look stick was). A quick
// tap queues one attack; PlayerAttack spends it, honoring its cooldown.
// Touch only; hidden without a touchscreen. Publishes its zone so LookDrag
// ignores presses that start on the button.
public class AttackButton : MonoBehaviour
{
    private static bool attackQueued;

    public static bool ConsumeAttack()
    {
        if (!attackQueued)
            return false;
        attackQueued = false;
        return true;
    }

    public static Vector2 ZoneCenterPx { get; private set; }
    public static float ZoneRadiusPx { get; private set; }

    private const float TapMaxSeconds = 0.3f;
    private const float TapMaxMovePx = 30f;

    private static readonly Color IdleColor = new Color(0.85f, 0.2f, 0.2f, 0.85f);
    private static readonly Color PressedColor = new Color(1f, 0.45f, 0.45f, 0.95f);

    private GameObject root;
    private RectTransform baseRt;
    private Image baseImage;
    private TouchControl activeTouch;
    private Touchscreen trackedScreen;
    private Vector2 pressPos;
    private float pressTime;

    private void Awake()
    {
        Build();
    }

    private void OnDestroy()
    {
        attackQueued = false;
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
            attackQueued = false;
            ZoneCenterPx = Vector2.zero;
            ZoneRadiusPx = 0f;
            activeTouch = null;
            return;
        }
        root.SetActive(true);

        Vector2 basePx = baseRt.position;
        float radiusPx = baseRt.rect.width * baseRt.lossyScale.x / 2f;
        ZoneCenterPx = basePx;
        ZoneRadiusPx = radiusPx;

        if (activeTouch != null && ReferenceEquals(screen, trackedScreen))
        {
            var phase = activeTouch.phase.ReadValue();
            if (phase == UnityEngine.InputSystem.TouchPhase.Ended)
            {
                Vector2 pos = activeTouch.position.ReadValue();
                if (Time.time - pressTime < TapMaxSeconds &&
                    Vector2.Distance(pos, pressPos) < TapMaxMovePx &&
                    (GameShell.Instance == null || GameShell.Instance.IsPlaying))
                    attackQueued = true;
                activeTouch = null;
            }
            else if (phase == UnityEngine.InputSystem.TouchPhase.Canceled ||
                     phase == UnityEngine.InputSystem.TouchPhase.None ||
                     phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                activeTouch = null; // Released, or slot reused by a new touch; rescan below.
            }
        }
        else
        {
            activeTouch = null;
            trackedScreen = screen;
        }
        if (activeTouch == null)
        {
            foreach (var touch in screen.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began &&
                    Vector2.Distance(touch.position.ReadValue(), basePx) < radiusPx)
                {
                    activeTouch = touch;
                    pressPos = touch.position.ReadValue();
                    pressTime = Time.time;
                    break;
                }
            }
        }
        baseImage.color = activeTouch != null ? PressedColor : IdleColor;
    }

    private void Build()
    {
        root = new GameObject("AttackButtonRoot");
        root.transform.SetParent(transform, false);

        GameObject canvasGo = new GameObject("AttackButtonCanvas");
        canvasGo.transform.SetParent(root.transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GameObject baseGo = new GameObject("Base");
        baseGo.transform.SetParent(canvasGo.transform, false);
        baseRt = baseGo.AddComponent<RectTransform>();
        baseRt.anchorMin = baseRt.anchorMax = new Vector2(1f, 0f);
        baseRt.pivot = new Vector2(0.5f, 0.5f);
        baseRt.sizeDelta = new Vector2(60f, 60f);
        baseRt.anchoredPosition = new Vector2(-60f, 80f);
        baseImage = baseGo.AddComponent<Image>();
        baseImage.sprite = CircleSprite.Get();
        baseImage.color = IdleColor;

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(baseGo.transform, false);
        RectTransform labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        Text label = labelGo.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 20;
        label.text = "ATK";
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
    }
}
