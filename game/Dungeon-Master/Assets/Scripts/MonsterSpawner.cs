using UnityEngine;

// Step 8: spawns the starting monsters. Positions are hand-placed inside the
// halls; counts come from the level config in step 10.
public class MonsterSpawner : MonoBehaviour
{
    private static readonly Vector3[] SpawnPoints = new Vector3[]
    {
        new Vector3(-2f, 1f, -7f),
        new Vector3(7f, 1f, 2f),
        new Vector3(24f, 1f, -4f),
    };

    private void Awake()
    {
        int count = Mathf.Min(GameConfig.Current.monsterCount, SpawnPoints.Length);
        for (int i = 0; i < count; i++)
            Spawn(SpawnPoints[i]);
    }

    public static void Spawn(Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Monster";
        go.transform.position = position;
        go.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        Object.Destroy(go.GetComponent<CapsuleCollider>());
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        go.GetComponent<Renderer>().material = new Material(shader) { color = new Color(0.8f, 0.15f, 0.15f) };
        go.AddComponent<CharacterController>();
        go.AddComponent<Monster>();
    }
}
