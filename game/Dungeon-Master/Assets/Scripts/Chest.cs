using UnityEngine;

// Step 2: unlocked chest. Walking up to it swings the lid open and pops out
// coins. Locks arrive in step 3.
public class Chest : Collectible
{
    [SerializeField] private int coinsInside = 5;

    private bool opened;
    private Transform lidPivot;

    protected override void Start()
    {
        base.Start();
        pickupRadius = 1.8f;
        lidPivot = transform.Find("LidPivot");
    }

    protected override void Update()
    {
        if (!opened && PlayerInRange())
            Open();
    }

    protected override void Animate()
    {
        // Chests stay put.
    }

    protected override void Collect()
    {
        // Unused: chests open instead of being collected.
    }

    private void Open()
    {
        opened = true;
        if (lidPivot != null)
            lidPivot.localRotation = Quaternion.Euler(-100f, 0f, 0f);
        for (int i = 0; i < coinsInside; i++)
        {
            float angle = i * Mathf.PI * 2f / coinsInside;
            Vector3 pos = transform.position + new Vector3(Mathf.Cos(angle) * 1.5f, 0.6f, Mathf.Sin(angle) * 1.5f);
            CollectiblePlacer.CreateCoin(pos);
        }
        Debug.Log($"Chest opened: {coinsInside} coins.");
    }
}
