using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Step 3: lock-puzzle popup. Built entirely in code so no scene setup is needed.
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
    private readonly Text[] optionLabels = new Text[3];
    private Chest currentChest;
    private MathPuzzle.Question current;
    private PlayerController playerController;
    private bool open;

    public void Show(Chest chest)
    {
        if (open)
            return;
        EnsureBuilt();
        currentChest = chest;
        current = MathPuzzle.Generate();
        questionText.text = current.text;
        for (int i = 0; i < 3; i++)
            optionLabels[i].text = current.options[i];
        feedbackText.text = "Pick the right answer to unlock.";
        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
            playerController.enabled = false;
        panel.SetActive(true);
        open = true;
    }

    private void OnAnswer(int index)
    {
        if (!open)
            return;
        if (index == current.correctIndex)
        {
            feedbackText.text = "Correct! The chest opens.";
            currentChest.UnlockAndOpen();
            StartCoroutine(CloseAfter(0.8f));
        }
        else
        {
            feedbackText.text = "Wrong - try again!";
        }
    }

    private IEnumerator CloseAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Close();
    }

    private void Close()
    {
        if (!open)
            return;
        open = false;
        panel.SetActive(false);
        if (playerController != null)
            playerController.enabled = true;
        playerController = null;
        currentChest = null;
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
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGo.transform, false);
        RectTransform pr = panel.AddComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(440f, 380f);
        pr.anchoredPosition = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.95f, 0.93f, 0.88f);

        MakeText(panel.transform, "Locked Chest", 26, new Vector2(0f, 145f), new Vector2(400f, 36f), font);
        questionText = MakeText(panel.transform, "?", 40, new Vector2(0f, 80f), new Vector2(400f, 56f), font);
        for (int i = 0; i < 3; i++)
            MakeButton(panel.transform, i, new Vector2(0f, 10f - i * 62f), font);
        feedbackText = MakeText(panel.transform, "", 20, new Vector2(0f, -163f), new Vector2(400f, 30f), font);
        panel.SetActive(false);
    }

    private void MakeButton(Transform parent, int index, Vector2 pos, Font font)
    {
        GameObject go = new GameObject("Option" + index);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400f, 52f);
        rt.anchoredPosition = pos;
        go.AddComponent<Image>().color = new Color(0.85f, 0.88f, 0.92f);
        Button button = go.AddComponent<Button>();
        optionLabels[index] = MakeText(go.transform, "?", 28, Vector2.zero, new Vector2(380f, 48f), font);
        int captured = index;
        button.onClick.AddListener(() => OnAnswer(captured));
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
        t.color = new Color(0.12f, 0.12f, 0.14f);
        t.alignment = TextAnchor.MiddleCenter;
        return t;
    }
}
