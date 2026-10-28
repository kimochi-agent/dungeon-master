using UnityEngine;

// Step 2: builds collectible visuals from primitives and scatters them
// through the dungeon. Runs from the bootstrap, alongside the generator.
// Step 3: adds locked chests (gold lids) holding the math puzzle.
// Step 10: spawn counts come from the level config; positions stay curated.
public class CollectiblePlacer : MonoBehaviour
{
    private static readonly Vector3[] CoinSpots = new Vector3[]
    {
        new Vector3(-3f, 0.6f, -3f),
        new Vector3(0f, 0.6f, 6f),
        new Vector3(3f, 0.6f, 0f),
        new Vector3(6f, 0.6f, -4f),
        new Vector3(-6f, 0.6f, 3f),
        new Vector3(12f, 0.6f, 0f),
        new Vector3(16f, 0.6f, 0f),
        new Vector3(20f, 0.6f, -4f),
        new Vector3(28f, 0.6f, 4f),
        new Vector3(30f, 0.6f, -4f),
    };

    private static readonly Vector3[] PowerSpots = new Vector3[]
    {
        new Vector3(6f, 0.6f, 6f),
        new Vector3(22f, 0.6f, 4f),
        new Vector3(29f, 0.6f, 0f),
    };

    private static readonly PowerUpKind[] PowerKinds = new PowerUpKind[]
    {
        PowerUpKind.Speed, PowerUpKind.Health, PowerUpKind.Shield,
    };

    private static readonly Vector3[] ChestSpots = new Vector3[]
    {
        new Vector3(-8f, 0f, 8f),
        new Vector3(30f, 0f, 5f),
    };

    private static readonly Vector3[] LockedSpots = new Vector3[]
    {
        new Vector3(8f, 0f, -8f),
        new Vector3(27f, 0f, -5f),
    };

    private void Awake()
    {
        LevelData cfg = GameConfig.Current;
        for (int i = 0; i < Mathf.Min(cfg.coinCount, CoinSpots.Length); i++)
            CreateCoin(CoinSpots[i]);
        for (int i = 0; i < Mathf.Min(cfg.powerUpCount, PowerSpots.Length); i++)
            CreatePowerUp(PowerSpots[i], PowerKinds[i % PowerKinds.Length]);
        for (int i = 0; i < Mathf.Min(cfg.chestCount, ChestSpots.Length); i++)
            CreateChest(ChestSpots[i]);
        for (int i = 0; i < Mathf.Min(cfg.lockedChestCount, LockedSpots.Length); i++)
            CreateChest(LockedSpots[i], true);
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

    public static GameObject CreateChest(Vector3 position, bool locked = false)
    {
        GameObject root = Root();
        GameObject chest = new GameObject(locked ? "LockedChest" : "Chest");
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
        Color lidColor = locked ? new Color(1f, 0.78f, 0.15f) : new Color(0.35f, 0.22f, 0.1f);
        lid.GetComponent<Renderer>().material = MakeMaterial(lidColor);

        chest.AddComponent<Chest>().isLocked = locked;
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
