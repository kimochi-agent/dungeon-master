using UnityEngine;

// Step 11: anything the math puzzle can unlock (chests, gates).
// One-strike rule: a wrong answer or timeout destroys DestroyOnFail locks.
public interface IPuzzleLock
{
    Transform LockTransform { get; }
    void UnlockAndOpen();
    bool DestroyOnFail { get; }
    float TimeLimitSeconds { get; } // Countdown; <= 0 means untimed.
    void OnPuzzleFailed();
    string GetTypeOfLock(); // "chest" or "gate".
}
