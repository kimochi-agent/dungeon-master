using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Step 6: pause, game-over/victory and restart for the Dungeon scene. Built
// in code. Home is a separate scene (HomeScreen); the run starts immediately
// on scene load. ShowGameOver(title, message) is the end-screen API.
public class GameShell : MonoBehaviour
{
    public static GameShell Instance { get; private set; }

    public bool IsPlaying { get; private set; }

    private GameObject pausePanel;
    private GameObject overPanel;
    private Text overTitle;
    private Text overText;
    private bool won; // Step 13 (additive).
    private DungeonGenerator dungeon; // Step 13 (additive).
    private Transform player; // Step 13 (additive).

    private void Awake()
    {
        Instance = this;
        EnsureEventSystem();
        Build();
        dungeon = FindFirstObjectByType<DungeonGenerator>(); // Step 13 (additive).
        var controller = FindFirstObjectByType<PlayerController>(); // Step 13 (additive).
        if (controller != null)
            player = controller.transform;
        StartGame();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        // P pauses (Esc is reserved: it exits pointer lock). First-person rework.
        if (keyboard != null && keyboard.pKey.wasPressedThisFrame)
        {
            if (IsPlaying && !pausePanel.activeSelf)
                Pause();
            else if (pausePanel.activeSelf)
                Resume();
        }

        // Step 13 (additive): reaching hall C clears the dungeon.
        if (IsPlaying && !won && dungeon != null && player != null &&
            Vector3.Distance(player.position, dungeon.ExitPoint) < 5f)
        {
            won = true;
            OnLevelComplete();
        }
    }

    private void OnLevelComplete()
    {
        LevelAnalytics.OnAttemptEnd(LevelEndReason.Completed);
        GameState.UnlockLevel(GameConfig.LevelIndex + 1);
        GoHome(); // Every completion returns home; Home offers the next level.
    }

    public void StartGame()
    {
        pausePanel.SetActive(false);
        overPanel.SetActive(false);
        Time.timeScale = 1f;
        IsPlaying = true;
        Cursor.lockState = CursorLockMode.Locked; // First-person: capture mouse.
        HUD.Instance?.ShowToast($"Level {GameConfig.LevelIndex}");
        LevelAnalytics.BeginLevel(GameConfig.LevelIndex, GameConfig.Current.levelName);
    }

    public void Pause()
    {
        if (!IsPlaying)
            return;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        IsPlaying = false;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Resume()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        IsPlaying = true;
        Cursor.lockState = CursorLockMode.Locked; // First-person: capture mouse.
    }

    // Play Again: reload the Dungeon scene straight back into the run.
    public void Restart()
    {
        LevelAnalytics.OnAttemptEnd(LevelEndReason.Abandoned);
        Time.timeScale = 1f;
        GameState.Reset();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Home: back to the home scene.
    public void GoHome()
    {
        LevelAnalytics.OnAttemptEnd(LevelEndReason.Abandoned);
        Time.timeScale = 1f;
        GameState.Reset();
        SceneManager.LoadScene(SceneNames.Home);
    }

    public void ShowGameOver(string message)
    {
        ShowGameOver("Run Over", message);
    }

    public void ShowGameOver(string title, string message)
    {
        overTitle.text = title;
        overText.text = message;
        pausePanel.SetActive(false);
        overPanel.SetActive(true);
        Time.timeScale = 0f;
        IsPlaying = false;
        Cursor.lockState = CursorLockMode.None;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    private void Build()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasGo = new GameObject("ShellCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        pausePanel = Panel(canvasGo.transform, "Pause", new Vector2(400f, 300f));
        MakeText(pausePanel.transform, "Paused", 34, new Vector2(0f, 80f), new Vector2(360f, 50f), font);
        MakeButton(pausePanel.transform, "Resume", new Vector2(0f, -10f), font, Resume);
        MakeButton(pausePanel.transform, "Restart", new Vector2(0f, -80f), font, Restart);

        overPanel = Panel(canvasGo.transform, "GameOver", new Vector2(480f, 320f));
        overTitle = MakeText(overPanel.transform, "", 38, new Vector2(0f, 105f), new Vector2(440f, 50f), font);
        overText = MakeText(overPanel.transform, "", 24, new Vector2(0f, 45f), new Vector2(440f, 70f), font);
        MakeButton(overPanel.transform, "Play Again", new Vector2(0f, -45f), font, Restart);
        MakeButton(overPanel.transform, "Home", new Vector2(0f, -112f), font, GoHome);

        // Small pause button, bottom-right, always visible during play.
        Button pauseButton = MakeButton(canvasGo.transform, "II", new Vector2(-16f, -16f), font, Pause);
        RectTransform rt = pauseButton.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(64f, 64f);
    }

    private GameObject Panel(Transform parent, string name, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        go.AddComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.95f);
        return go;
    }

    private Button MakeButton(Transform parent, string label, Vector2 pos, Font font, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject("Button_" + label);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300f, 56f);
        rt.anchoredPosition = pos;
        go.AddComponent<Image>().color = new Color(0.16f, 0.42f, 0.31f);
        Button button = go.AddComponent<Button>();
        Text t = MakeText(go.transform, label, 26, Vector2.zero, new Vector2(280f, 50f), font);
        t.color = Color.white;
        button.onClick.AddListener(onClick);
        return button;
    }

    private Text MakeText(Transform parent, string text, int size, Vector2 pos, Vector2 sizeDelta, Font font)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = pos;
        Text t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.text = text;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        return t;
    }
}
