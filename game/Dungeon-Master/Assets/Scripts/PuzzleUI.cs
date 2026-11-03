using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Step 3: lock-puzzle popup. Built entirely in code so no scene setup is needed.
// Compact fractional panel (never fullscreen) with a skip/close button; a
// skipped lock stays shut until the player walks away and returns.
public class PuzzleUI : MonoBehaviour
{
    private static PuzzleUI instance;

    public static PuzzleUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PuzzleUI>();
                if (instance == null)
                    instance = new GameObject("PuzzleUI").AddComponent<PuzzleUI>();
            }
            return instance;
        }
    }

    private GameObject panel;
    private Text questionText;
    private Text feedbackText;
    private Text timerLabel;
    private float timeLimit;
    private float timeLeft;
    private bool resolved;
    private readonly Text[] optionLabels = new Text[3];
    private IPuzzleLock currentLock;
    private IPuzzleLock dismissedLock;
    private MathPuzzle.Question current;
    private PlayerController playerController;
    private Transform playerTransform;
    private bool open;

    // True while the player is engaged with a chest/gate puzzle. Static form
    // avoids instantiating the lazy singleton just to ask.
    public static bool IsOpen => instance != null && instance.open;

    public void Show(IPuzzleLock puzzleLock)
    {
        if (open || puzzleLock == null || puzzleLock == dismissedLock)
            return;
        if (GameShell.Instance != null && !GameShell.Instance.IsPlaying)
            return; // No popups over pause/end screens.
        EnsureBuilt();
        currentLock = puzzleLock;
        current = MathPuzzle.Generate();
        questionText.text = current.text;
        for (int i = 0; i < 3; i++)
            optionLabels[i].text = current.options[i];
        feedbackText.text = "Pick the right answer to unlock.";
        timeLimit = puzzleLock.TimeLimitSeconds;
        timeLeft = timeLimit;
        resolved = false;
        timerLabel.gameObject.SetActive(timeLimit > 0f);
        UpdateTimerLabel();
        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
            playerController.enabled = false;
        panel.SetActive(true);
        open = true;
    }

    private void Update()
    {
        if (open && !resolved && timeLimit > 0f)
        {
            timeLeft -= Time.deltaTime;
            UpdateTimerLabel();
            if (timeLeft <= 0f)
            {
                if (currentLock != null && currentLock.DestroyOnFail)
                    FailLock("Too slow! The chest is destroyed.");
                else
                {
                    HUD.Instance?.ShowToast("Too slow!");
                    Close(true);
                }
                return;
            }
        }

        // Re-arm a skipped lock once the player walks away from it.
        if (open || dismissedLock == null)
            return;
        if (dismissedLock as Object == null)
        {
            dismissedLock = null;
            return;
        }
        if (playerTransform == null)
        {
            var controller = FindFirstObjectByType<PlayerController>();
            if (controller != null)
                playerTransform = controller.transform;
        }
        if (playerTransform != null &&
            Vector3.Distance(playerTransform.position, dismissedLock.LockTransform.position) > 4f)
            dismissedLock = null;
    }

    private void OnAnswer(int index)
    {
        if (!open || resolved)
            return;
        if (index == current.correctIndex)
        {
            resolved = true;
            feedbackText.text = "Correct! Unlocked.";
            currentLock.UnlockAndOpen();
            StartCoroutine(CloseAfter(0.8f));
        }
        else if (currentLock != null && currentLock.DestroyOnFail)
        {
            FailLock("Wrong! The chest is destroyed.");
        }
        else
        {
            feedbackText.text = "Wrong - try again!";
        }
    }

    private IEnumerator CloseAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds); // Unscaled: pause can't wedge us open.
        Close(false);
    }

    private void FailLock(string toast)
    {
        resolved = true;
        HUD.Instance?.ShowToast(toast);
        currentLock?.OnPuzzleFailed();
        Close(true);
    }

    private void UpdateTimerLabel()
    {
        timerLabel.text = $"{Mathf.Max(0, Mathf.CeilToInt(timeLeft))}s";
    }

    private void Close(bool skipped)
    {
        if (!open)
            return;
        if (skipped)
            dismissedLock = currentLock;
        open = false;
        panel.SetActive(false);
        if (playerController != null)
            playerController.enabled = true;
        playerController = null;
        currentLock = null;
    }

    private void EnsureBuilt()
    {
        if (panel != null)
            return;
        if (EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasGo = new GameObject("PuzzleCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Fractional panel: ~76% wide, ~50% tall, dungeon stays visible.
        panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGo.transform, false);
        RectTransform pr = panel.AddComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.12f, 0.28f);
        pr.anchorMax = new Vector2(0.88f, 0.78f);
        pr.offsetMin = Vector2.zero;
        pr.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.95f, 0.93f, 0.88f);

        MakeRowText(panel.transform, "Locked Chest", 26, 0.87f, 0.97f, font);
        timerLabel = MakeRowText(panel.transform, "10s", 24, 0.79f, 0.86f, font);
        timerLabel.color = new Color(0.8f, 0.15f, 0.15f);
        questionText = MakeRowText(panel.transform, "?", 36, 0.60f, 0.78f, font);
        for (int i = 0; i < 3; i++)
            MakeRowButton(panel.transform, i, 0.45f - i * 0.15f, font);
        feedbackText = MakeRowText(panel.transform, "", 20, 0.02f, 0.12f, font);
        MakeCloseButton(panel.transform, font);
        panel.SetActive(false);
    }

    private void MakeRowButton(Transform parent, int index, float yMin, Font font)
    {
        GameObject go = new GameObject("Option" + index);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.06f, yMin);
        rt.anchorMax = new Vector2(0.94f, yMin + 0.13f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = new Color(0.85f, 0.88f, 0.92f);
        Button button = go.AddComponent<Button>();
        optionLabels[index] = MakeRowText(parent, "?", 28, yMin, yMin + 0.13f, font);
        optionLabels[index].raycastTarget = false;
        optionLabels[index].transform.SetAsLastSibling();
        int captured = index;
        button.onClick.AddListener(() => OnAnswer(captured));
    }

    private void MakeCloseButton(Transform parent, Font font)
    {
        GameObject go = new GameObject("Close");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(56f, 56f);
        rt.anchoredPosition = new Vector2(-10f, -10f);
        go.AddComponent<Image>().color = new Color(0.7f, 0.2f, 0.2f);
        Text label = MakeRowText(go.transform, "X", 28, 0f, 1f, font);
        label.color = Color.white;
        go.AddComponent<Button>().onClick.AddListener(() => Close(true));
    }

    private Text MakeRowText(Transform parent, string text, int size, float yMin, float yMax, Font font)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.04f, yMin);
        rt.anchorMax = new Vector2(0.96f, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Text t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize = 8;
        t.resizeTextMaxSize = size;
        t.text = text;
        t.color = new Color(0.12f, 0.12f, 0.14f);
        t.alignment = TextAnchor.MiddleCenter;
        return t;
    }
}
