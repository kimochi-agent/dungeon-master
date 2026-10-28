using UnityEngine;

// Step 11: anything the math puzzle can unlock (chests, gates).
public interface IPuzzleLock
{
    Transform LockTransform { get; }
    void UnlockAndOpen();
}
