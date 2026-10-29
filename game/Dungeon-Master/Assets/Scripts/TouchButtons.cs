using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;

// Right-side button cluster: 4 discrete movement buttons around a center
// attack button. Touch only; hidden unless a touchscreen exists.
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
        public Vector2 screenCenter;
        public float radiusPx;
        public bool pressed;
        public Color idleColor = new Color(1f, 1f, 1f, 0.25f);
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
            InputTouchPhase phase = touch.phase.ReadValue();
            Vector2 pos = touch.position.ReadValue();
            if (phase != InputTouchPhase.Began && phase != InputTouchPhase.Moved &&
                phase != InputTouchPhase.Stationary)
            {
                if (id == attackTouchId)
                {
                    if (phase == InputTouchPhase.Ended &&
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
            else if (phase == InputTouchPhase.Began && Inside(attack, pos))
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
        return Vector2.Distance(screenPos, button.screenCenter) < button.radiusPx;
    }

    private static void Refresh(PadButton button)
    {
        button.image.color = button.pressed
            ? new Color(1f, 1f, 1f, 0.6f)
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
        float s = canvas.scaleFactor;

        // Cluster center, bottom-right; directions ring it, attack sits middle.
        Vector2 center = new Vector2(-190f, 200f);
        MakeButton(canvasGo.transform, attack, center, 120f, new Color(1f, 0.35f, 0.35f, 0.55f), s);
        MakeButton(canvasGo.transform, up, center + new Vector2(0f, 120f), 96f, up.idleColor, s);
        MakeButton(canvasGo.transform, down, center + new Vector2(0f, -120f), 96f, down.idleColor, s);
        MakeButton(canvasGo.transform, left, center + new Vector2(-120f, 0f), 96f, left.idleColor, s);
        MakeButton(canvasGo.transform, right, center + new Vector2(120f, 0f), 96f, right.idleColor, s);
    }

    private void MakeButton(Transform parent, PadButton button, Vector2 offset, float size, Color color, float scale)
    {
        GameObject go = new GameObject("PadButton");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = offset;
        button.image = go.AddComponent<Image>();
        button.image.sprite = CircleSprite.Get();
        button.image.color = color;
        button.idleColor = color;
        button.screenCenter = new Vector2(Screen.width + offset.x * scale, offset.y * scale);
        button.radiusPx = size / 2f * scale;
    }
}
