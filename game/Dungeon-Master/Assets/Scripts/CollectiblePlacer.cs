using UnityEngine;

// Step 2: builds collectible visuals from primitives and scatters them
// through the dungeon. Runs from the bootstrap, alongside the generator.
// Step 3: adds locked chests (gold lids) holding the math puzzle.
// Step 10: spawn counts come from the level config; positions stay curated.
public class CollectiblePlacer : MonoBehaviour
{
    private static readonly PowerUpKind[] PowerKinds = new PowerUpKind[]
    {
        PowerUpKind.Speed, PowerUpKind.Health, PowerUpKind.Shield,
    };

    private void Awake()
    {
        LevelData cfg = GameConfig.Current;
        for (int i = 0; i < Mathf.Min(cfg.coinCount, cfg.coinSpots.Length); i++)
            CreateCoin(cfg.coinSpots[i]);
        for (int i = 0; i < Mathf.Min(cfg.powerUpCount, cfg.powerUpSpots.Length); i++)
            CreatePowerUp(cfg.powerUpSpots[i], PowerKinds[i % PowerKinds.Length]);
        for (int i = 0; i < Mathf.Min(cfg.chestCount, cfg.chestSpots.Length); i++)
            CreateChest(cfg.chestSpots[i]);
        for (int i = 0; i < Mathf.Min(cfg.lockedChestCount, cfg.lockedChestSpots.Length); i++)
            CreateChest(cfg.lockedChestSpots[i], true);
    }

    public static GameObject CreateCoin(Vector3 position)
    {
        GameObject go = CoinVisual.Create(); // Mesh + finish live in Visuals/.
        go.transform.SetParent(Root().transform, false);
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
        Renderer bodyRenderer = body.GetComponent<Renderer>();
        bodyRenderer.sharedMaterial = SharedMaterials.Tinted(bodyRenderer, new Color(0.5f, 0.32f, 0.15f));

        GameObject pivot = new GameObject("LidPivot");
        pivot.transform.SetParent(chest.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 0.7f, -0.4f);

        GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lid.name = "Lid";
        lid.transform.SetParent(pivot.transform, false);
        lid.transform.localPosition = new Vector3(0f, 0.125f, 0.4f);
        lid.transform.localScale = new Vector3(1.2f, 0.25f, 0.8f);
        Color lidColor = locked ? new Color(1f, 0.78f, 0.15f) : new Color(0.35f, 0.22f, 0.1f);
        Renderer lidRenderer = lid.GetComponent<Renderer>();
        lidRenderer.sharedMaterial = SharedMaterials.Tinted(lidRenderer, lidColor);

        chest.AddComponent<Chest>().isLocked = locked;
        return chest;
    }

    private static GameObject Primitive(GameObject go, PrimitiveType type, Vector3 scale, Color color)
    {
        GameObject mesh = GameObject.CreatePrimitive(type);
        mesh.transform.SetParent(go.transform, false);
        mesh.transform.localScale = scale;
        Renderer renderer = mesh.GetComponent<Renderer>();
        renderer.sharedMaterial = SharedMaterials.Tinted(renderer, color);
        go.transform.SetParent(Root().transform, false);
        return go;
    }

    internal static GameObject Root()
    {
        GameObject root = GameObject.Find("Collectibles");
        if (root == null)
            root = new GameObject("Collectibles");
        return root;
    }
}
