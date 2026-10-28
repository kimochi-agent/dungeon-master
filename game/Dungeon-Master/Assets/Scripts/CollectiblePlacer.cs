using UnityEngine;

// Step 2: builds collectible visuals from primitives and scatters them
// through the dungeon. Runs from the bootstrap, alongside the generator.
public class CollectiblePlacer : MonoBehaviour
{
    private void Awake()
    {
        // Hall A coins.
        CreateCoin(new Vector3(-3f, 0.6f, -3f));
        CreateCoin(new Vector3(0f, 0.6f, 6f));
        CreateCoin(new Vector3(3f, 0.6f, 0f));
        CreateCoin(new Vector3(6f, 0.6f, -4f));
        CreateCoin(new Vector3(-6f, 0.6f, 3f));
        // Corridor coins.
        CreateCoin(new Vector3(12f, 0.6f, 0f));
        CreateCoin(new Vector3(16f, 0.6f, 0f));
        // Hall B coins.
        CreateCoin(new Vector3(20f, 0.6f, -4f));
        CreateCoin(new Vector3(28f, 0.6f, 4f));
        CreateCoin(new Vector3(30f, 0.6f, -4f));
        // Power-ups (effects in step 7).
        CreatePowerUp(new Vector3(6f, 0.6f, 6f), PowerUpKind.Speed);
        CreatePowerUp(new Vector3(22f, 0.6f, 4f), PowerUpKind.Health);
        CreatePowerUp(new Vector3(29f, 0.6f, 0f), PowerUpKind.Shield);
        // Unlocked chests.
        CreateChest(new Vector3(-8f, 0f, 8f));
        CreateChest(new Vector3(30f, 0f, 5f));
    }

    public static GameObject CreateCoin(Vector3 position)
    {
        GameObject go = Primitive(new GameObject("Coin"), PrimitiveType.Cylinder,
            new Vector3(0.55f, 0.1f, 0.55f), new Color(1f, 0.78f, 0.15f));
        go.transform.position = position;
        go.AddComponent<Coin>();
        return go;
    }

    public static GameObject CreatePowerUp(Vector3 position, PowerUpKind kind)
    {
        Color color = kind switch
        {
            PowerUpKind.Speed => new Color(0.2f, 0.9f, 0.3f),
            PowerUpKind.Health => new Color(0.9f, 0.25f, 0.25f),
            _ => new Color(0.25f, 0.5f, 1f),
        };
        GameObject go = Primitive(new GameObject(kind + "PowerUp"), PrimitiveType.Sphere,
            new Vector3(0.7f, 0.7f, 0.7f), color);
        go.transform.position = position;
        go.AddComponent<PowerUp>().kind = kind;
        return go;
    }

    public static GameObject CreateChest(Vector3 position)
    {
        GameObject root = Root();
        GameObject chest = new GameObject("Chest");
        chest.transform.SetParent(root.transform, false);
        chest.transform.position = position;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(chest.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        body.transform.localScale = new Vector3(1.2f, 0.7f, 0.8f);
        body.GetComponent<Renderer>().material = MakeMaterial(new Color(0.5f, 0.32f, 0.15f));

        GameObject pivot = new GameObject("LidPivot");
        pivot.transform.SetParent(chest.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 0.7f, -0.4f);

        GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lid.name = "Lid";
        lid.transform.SetParent(pivot.transform, false);
        lid.transform.localPosition = new Vector3(0f, 0.125f, 0.4f);
        lid.transform.localScale = new Vector3(1.2f, 0.25f, 0.8f);
        lid.GetComponent<Renderer>().material = MakeMaterial(new Color(0.35f, 0.22f, 0.1f));

        chest.AddComponent<Chest>();
        return chest;
    }

    private static GameObject Primitive(GameObject go, PrimitiveType type, Vector3 scale, Color color)
    {
        GameObject mesh = GameObject.CreatePrimitive(type);
        mesh.transform.SetParent(go.transform, false);
        mesh.transform.localScale = scale;
        mesh.GetComponent<Renderer>().material = MakeMaterial(color);
        go.transform.SetParent(Root().transform, false);
        return go;
    }

    private static GameObject Root()
    {
        GameObject root = GameObject.Find("Collectibles");
        if (root == null)
            root = new GameObject("Collectibles");
        return root;
    }

    private static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader);
        material.color = color;
        return material;
    }
}
