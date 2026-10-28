using UnityEngine;

// Base pickup: gentle spin/bob, collected by player proximity.
// (Distance check instead of trigger physics: robust with CharacterController.)
public abstract class Collectible : MonoBehaviour
{
    [SerializeField] protected float pickupRadius = 1.2f;
    [SerializeField] private float spinSpeed = 90f;
    [SerializeField] private float bobHeight = 0.15f;

    protected Transform player;
    protected bool collected;
    private Vector3 basePosition;

    protected virtual void Start()
    {
        basePosition = transform.position;
        var controller = FindFirstObjectByType<PlayerController>();
        if (controller != null)
            player = controller.transform;
    }

    protected virtual void Update()
    {
        Animate();
        if (!collected && PlayerInRange())
        {
            collected = true;
            Collect();
            Destroy(gameObject);
        }
    }

    protected virtual void Animate()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * 2f) * bobHeight);
    }

    protected bool PlayerInRange()
    {
        return player != null && Vector3.Distance(transform.position, player.position) < pickupRadius;
    }

    protected abstract void Collect();
}
