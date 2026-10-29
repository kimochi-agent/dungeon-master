using UnityEngine;
using UnityEngine.InputSystem;
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
    private int activeTouchId = -1;
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
        activeTouchId = -1;
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
            activeTouchId = -1;
            return;
        }
        root.SetActive(true);

        Vector2 basePx = baseRt.position;
        float radiusPx = baseRt.rect.width * baseRt.lossyScale.x / 2f;
        ZoneCenterPx = basePx;
        ZoneRadiusPx = radiusPx;

        bool found = false;
        foreach (var touch in screen.touches)
        {
            int id = touch.touchId.ReadValue();
            var phase = touch.phase.ReadValue();
            if (id == activeTouchId)
            {
                found = true;
                if (phase == UnityEngine.InputSystem.TouchPhase.Ended)
                {
                    Vector2 pos = touch.position.ReadValue();
                    if (Time.time - pressTime < TapMaxSeconds &&
                        Vector2.Distance(pos, pressPos) < TapMaxMovePx &&
                        (GameShell.Instance == null || GameShell.Instance.IsPlaying))
                        attackQueued = true;
                    activeTouchId = -1;
                }
                else if (phase == UnityEngine.InputSystem.TouchPhase.Canceled ||
                         phase == UnityEngine.InputSystem.TouchPhase.None)
                {
                    activeTouchId = -1;
                }
            }
            else if (activeTouchId == -1 && phase == UnityEngine.InputSystem.TouchPhase.Began &&
                     Vector2.Distance(touch.position.ReadValue(), basePx) < radiusPx)
            {
                activeTouchId = id;
                pressPos = touch.position.ReadValue();
                pressTime = Time.time;
                found = true;
            }
        }
        if (activeTouchId != -1 && !found)
            activeTouchId = -1;
        baseImage.color = activeTouchId != -1 ? PressedColor : IdleColor;
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
        baseRt.sizeDelta = new Vector2(120f, 120f);
        baseRt.anchoredPosition = new Vector2(-95f, 105f);
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
        label.fontSize = 30;
        label.text = "ATK";
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
    }
}
