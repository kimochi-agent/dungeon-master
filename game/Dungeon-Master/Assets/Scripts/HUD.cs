using UnityEngine;
using UnityEngine.UI;

// Step 4: health bar plus coin/power-up counters. Built in code, read fresh
// every frame from PlayerHealth and GameState.
public class HUD : MonoBehaviour
{
    public static HUD Instance { get; private set; }

    private RectTransform fillRt;
    private Text coinText;
    private Text powerText;
    private Text effectText;
    private Text toastText;
    private float toastUntil;
    private PlayerHealth health;
    private PlayerController playerController;

    private void Awake()
    {
        Instance = this;
        Build();
        health = FindFirstObjectByType<PlayerHealth>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ShowToast(string message)
    {
        toastText.text = message;
        toastText.gameObject.SetActive(true);
        toastUntil = Time.unscaledTime + 2f;
    }

    private void Update()
    {
        if (health == null)
            health = FindFirstObjectByType<PlayerHealth>();
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();
        if (health != null)
        {
            float fraction = Mathf.Clamp01((float)health.CurrentHealth / health.MaxHealth);
            fillRt.anchorMax = new Vector2(fraction, 1f); // Green width = health; red backing = missing.
        }
        coinText.text = $"Coins: {GameState.Coins}";
        powerText.text = $"Power-ups: {GameState.PowerUps.Count}";
        string effect = "";
        if (health != null && health.shieldHits > 0)
            effect = $"Shield x{health.shieldHits}";
        if (playerController != null && playerController.SpeedBoostActive)
            effect = effect == "" ? "SPEED!" : effect + "  SPEED!";
        effectText.text = effect;
        if (toastText.gameObject.activeSelf && Time.unscaledTime > toastUntil)
            toastText.gameObject.SetActive(false);
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
        barBg.AddComponent<Image>().color = new Color(0.75f, 0.12f, 0.12f, 0.9f);

        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(barBg.transform, false);
        fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        fillGo.AddComponent<Image>().color = Color.green;

        coinText = MakeText(canvasGo.transform, "Coins: 0", new Vector2(-96f, -16f), font);
        powerText = MakeText(canvasGo.transform, "Power-ups: 0", new Vector2(-96f, -48f), font);
        effectText = MakeText(canvasGo.transform, "", new Vector2(-96f, -80f), font);

        GameObject toastGo = new GameObject("Toast");
        toastGo.transform.SetParent(canvasGo.transform, false);
        RectTransform toastRt = toastGo.AddComponent<RectTransform>();
        toastRt.anchorMin = new Vector2(0.5f, 1f);
        toastRt.anchorMax = new Vector2(0.5f, 1f);
        toastRt.pivot = new Vector2(0.5f, 1f);
        toastRt.sizeDelta = new Vector2(500f, 40f);
        toastRt.anchoredPosition = new Vector2(0f, -70f);
        toastText = toastGo.AddComponent<Text>();
        toastText.font = font;
        toastText.fontSize = 26;
        toastText.alignment = TextAnchor.MiddleCenter;
        toastText.color = new Color(1f, 0.9f, 0.4f);
        toastGo.SetActive(false);
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
