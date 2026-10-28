using UnityEngine;
using UnityEngine.UI;

// Step 4: health bar plus coin/power-up counters. Built in code, read fresh
// every frame from PlayerHealth and GameState.
public class HUD : MonoBehaviour
{
    private Image healthFill;
    private Text coinText;
    private Text powerText;
    private PlayerHealth health;

    private void Awake()
    {
        Build();
        health = FindFirstObjectByType<PlayerHealth>();
    }

    private void Update()
    {
        if (health != null)
        {
            float fraction = (float)health.CurrentHealth / health.MaxHealth;
            healthFill.fillAmount = fraction;
            healthFill.color = Color.Lerp(Color.red, Color.green, fraction);
        }
        coinText.text = $"Coins: {GameState.Coins}";
        powerText.text = $"Power-ups: {GameState.PowerUps.Count}";
    }

    private void Build()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasGo = new GameObject("HUDCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject barBg = new GameObject("HealthBar");
        barBg.transform.SetParent(canvasGo.transform, false);
        RectTransform bgRt = barBg.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 1f);
        bgRt.anchorMax = new Vector2(0f, 1f);
        bgRt.pivot = new Vector2(0f, 1f);
        bgRt.anchoredPosition = new Vector2(16f, -16f);
        bgRt.sizeDelta = new Vector2(260f, 28f);
        barBg.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 0.8f);

        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(barBg.transform, false);
        RectTransform fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(3f, 3f);
        fillRt.offsetMax = new Vector2(-3f, -3f);
        healthFill = fillGo.AddComponent<Image>();
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        healthFill.color = Color.green;

        coinText = MakeText(canvasGo.transform, "Coins: 0", new Vector2(-16f, -16f), font);
        powerText = MakeText(canvasGo.transform, "Power-ups: 0", new Vector2(-16f, -48f), font);
    }

    private Text MakeText(Transform parent, string text, Vector2 pos, Font font)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(300f, 28f);
        rt.anchoredPosition = pos;
        Text t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = 22;
        t.text = text;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleRight;
        return t;
    }
}
