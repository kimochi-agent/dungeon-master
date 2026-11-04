using System.Collections;
using UnityEngine;

// Step 11: puzzle-locked gate blocking a dungeon doorway. Unlock via the math
// popup, then sinks into the floor.
public class Gate : MonoBehaviour, IPuzzleLock
{
    public Transform LockTransform => transform;
    public bool DestroyOnFail => false; // Gates stay forgiving: retry freely.
    public float TimeLimitSeconds => 0f; // Untimed.

    public void OnPuzzleFailed() { }

    private bool opened;
    private Transform player;

    private void Start()
    {
        var controller = FindFirstObjectByType<PlayerController>();
        if (controller != null)
            player = controller.transform;
    }

    private void Update()
    {
        if (!opened && player != null && Vector3.Distance(transform.position, player.position) < 2.5f)
            PuzzleUI.Instance.Show(this);
    }

    public void UnlockAndOpen()
    {
        if (opened)
            return;
        opened = true;
        LevelAnalytics.RecordGateOpened();
        StartCoroutine(Sink());
    }

    public string GetTypeOfLock()
    {
        return "Gate";
    }

    private IEnumerator Sink()
    {
        Vector3 start = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            transform.position = start + Vector3.down * 3f * Mathf.Min(t, 1f);
            yield return null;
        }
        var collider = GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;
    }
}
