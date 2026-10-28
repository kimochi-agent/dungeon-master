using UnityEngine;

// Step 1: keeps the camera above and behind the player.
public class FollowCamera : MonoBehaviour
{
    public Transform target;

    [SerializeField] private Vector3 offset = new Vector3(0f, 14f, -9f);

    private void LateUpdate()
    {
        if (target == null)
            return;
        transform.position = target.position + offset;
        transform.LookAt(target.position);
    }
}
