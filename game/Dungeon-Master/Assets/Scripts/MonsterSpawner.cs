using UnityEngine;

// Step 8: spawns the starting monsters. Positions are hand-placed inside the
// halls; counts come from the level config in step 10.
public class MonsterSpawner : MonoBehaviour
{
    private void Awake()
    {
        LevelData cfg = GameConfig.Current;
        int count = Mathf.Min(cfg.monsterCount, cfg.monsterSpots.Length);
        for (int i = 0; i < count; i++)
            Spawn(cfg.monsterSpots[i]);
    }

    public static void Spawn(Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Monster";
        go.transform.position = position;
        go.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        Object.Destroy(go.GetComponent<CapsuleCollider>());
        Renderer monsterRenderer = go.GetComponent<Renderer>();
        monsterRenderer.sharedMaterial = SharedMaterials.Tinted(monsterRenderer, new Color(0.8f, 0.15f, 0.15f));
        MonsterVisual.Attach(go); // Eyes. Pure visuals; Monster.cs untouched.
        go.AddComponent<CharacterController>();
        go.AddComponent<Monster>();
    }
}
