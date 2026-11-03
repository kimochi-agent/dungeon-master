using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// UNUSED (legacy): triangle direction-button cluster for movement around a
// circular attack button. Kept in the repo but never added to the scene —
// MoveJoystick (analog stick, tap to attack) replaced it after touch issues.
public class TouchButtons : MonoBehaviour
{
    public static Vector2 MoveVector { get; private set; }

    private static bool attackQueued;

    public static bool ConsumeAttack()
    {
        if (!attackQueued)
            return false;
        attackQueued = false;
        return true;
    }

    private class PadButton
    {
        public Image image;
        public RectTransform rect;
        public bool pressed;
        public Color idleColor;
    }

    private GameObject root;
    private PadButton up = new PadButton();
    private PadButton down = new PadButton();
    private PadButton left = new PadButton();
    private PadButton right = new PadButton();
    private PadButton attack = new PadButton();
    private int attackTouchId = -1;
    private Vector2 attackPos;
    private float attackTime;

    private void Awake()
    {
        Build();
    }

    private void OnDestroy()
    {
        MoveVector = Vector2.zero;
        attackQueued = false;
    }

    private void Update()
    {
        var screen = Touchscreen.current;
        if (screen == null)
        {
            root.SetActive(false);
            MoveVector = Vector2.zero;
            attackQueued = false;
            attackTouchId = -1;
            return;
        }
        root.SetActive(true);

        up.pressed = down.pressed = left.pressed = right.pressed = false;
        foreach (var touch in screen.touches)
        {
            int id = touch.touchId.ReadValue();
            var phase = touch.phase.ReadValue();
            Vector2 pos = touch.position.ReadValue();
            bool active = phase == UnityEngine.InputSystem.TouchPhase.Began ||
                          phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                          phase == UnityEngine.InputSystem.TouchPhase.Stationary;
            if (!active)
            {
                if (id == attackTouchId)
                {
                    if (phase == UnityEngine.InputSystem.TouchPhase.Ended &&
                        Time.time - attackTime < 0.35f &&
                        Vector2.Distance(pos, attackPos) < 40f)
                        attackQueued = true;
                    attackTouchId = -1;
                }
                continue;
            }
            if (Inside(up, pos)) up.pressed = true;
            if (Inside(down, pos)) down.pressed = true;
            if (Inside(left, pos)) left.pressed = true;
            if (Inside(right, pos)) right.pressed = true;
            if (id == attackTouchId)
            {
                // Held: wait for release to judge tap vs drag-away.
            }
            else if (phase == UnityEngine.InputSystem.TouchPhase.Began && Inside(attack, pos))
            {
                attackTouchId = id;
                attackPos = pos;
                attackTime = Time.time;
            }
        }
        attack.pressed = attackTouchId != -1;
        MoveVector = new Vector2(
            (right.pressed ? 1f : 0f) - (left.pressed ? 1f : 0f),
            (up.pressed ? 1f : 0f) - (down.pressed ? 1f : 0f));
        Refresh(up);
        Refresh(down);
        Refresh(left);
        Refresh(right);
        Refresh(attack);
    }

    private static bool Inside(PadButton button, Vector2 screenPos)
    {
        float radiusPx = button.rect.rect.width * button.rect.lossyScale.x / 2f;
        return Vector2.Distance(screenPos, (Vector2)button.rect.position) < radiusPx;
    }

    private static void Refresh(PadButton button)
    {
        button.image.color = button.pressed
            ? new Color(1f, 1f, 1f, 0.75f)
            : button.idleColor;
    }

    private void Build()
    {
        root = new GameObject("ButtonsRoot");
        root.transform.SetParent(transform, false);

        GameObject canvasGo = new GameObject("ButtonsCanvas");
        canvasGo.transform.SetParent(root.transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        Vector2 center = new Vector2(115f, 120f);
        MakeButton(canvasGo.transform, attack, center, 32f, CircleSprite.Get(), 0f,
            new Color(0.85f, 0.2f, 0.2f, 0.75f));
        MakeButton(canvasGo.transform, up, center + new Vector2(0f, 40f), 40f, TriangleSprite.Get(), 0f,
            new Color(0.85f, 0.85f, 0.88f, 0.55f));
        MakeButton(canvasGo.transform, down, center + new Vector2(0f, -40f), 40f, TriangleSprite.Get(), 180f,
            new Color(0.85f, 0.85f, 0.88f, 0.55f));
        MakeButton(canvasGo.transform, left, center + new Vector2(-40f, 0f), 40f, TriangleSprite.Get(), 90f,
            new Color(0.85f, 0.85f, 0.88f, 0.55f));
        MakeButton(canvasGo.transform, right, center + new Vector2(40f, 0f), 40f, TriangleSprite.Get(), -90f,
            new Color(0.85f, 0.85f, 0.88f, 0.55f));
    }

    private void MakeButton(Transform parent, PadButton button, Vector2 offset, float size, Sprite sprite, float rotZ, Color color)
    {
        GameObject go = new GameObject("PadButton");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = offset;
        button.rect = rt;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        RectTransform vrt = visual.AddComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = Vector2.zero;
        vrt.offsetMax = Vector2.zero;
        vrt.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        button.image = visual.AddComponent<Image>();
        button.image.sprite = sprite;
        button.image.color = color;
        button.idleColor = color;
    }
}
