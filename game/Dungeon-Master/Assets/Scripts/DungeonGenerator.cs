using UnityEngine;

// Step 1: builds a small explorable dungeon (two halls joined by a corridor)
// out of primitives. Builds itself on Awake when empty.
public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private float wallHeight = 3f;
    [SerializeField] private float wallThickness = 1f;

    public Vector3 SpawnPoint = new Vector3(-6f, 1f, 0f);
    public Vector3 HallCCenter { get; private set; } // Step 13 (additive).

    private Material floorMaterial;
    private Material wallMaterial;

    private void Awake()
    {
        if (transform.childCount == 0)
            Build();
    }

    public void Build()
    {
        floorMaterial = MakeMaterial(new Color(0.32f, 0.30f, 0.34f));
        wallMaterial = MakeMaterial(new Color(0.45f, 0.32f, 0.24f));

        // Hall A: 20x20 centered on origin, doorway on the east side.
        AddBox("FloorA", new Vector3(0f, -0.25f, 0f), new Vector3(20f, 0.5f, 20f), floorMaterial);
        AddWall(new Vector3(0f, 0f, 10f), 20f, false);   // north
        AddWall(new Vector3(0f, 0f, -10f), 20f, false);  // south
        AddWall(new Vector3(-10f, 0f, 0f), 20f, true);   // west
        AddWallWithDoor(new Vector3(10f, 0f, 0f), 20f, true); // east with doorway

        // Corridor: x from 10 to 18, 4 wide.
        AddBox("FloorCorridor", new Vector3(14f, -0.25f, 0f), new Vector3(8f, 0.5f, 4f), floorMaterial);
        AddBox("WallCorridorN", new Vector3(14f, wallHeight / 2f, 2.5f), new Vector3(8f, wallHeight, wallThickness), wallMaterial);
        AddBox("WallCorridorS", new Vector3(14f, wallHeight / 2f, -2.5f), new Vector3(8f, wallHeight, wallThickness), wallMaterial);

        // Hall B: 14x14 centered on x=25, doorway on the west side.
        AddBox("FloorB", new Vector3(25f, -0.25f, 0f), new Vector3(14f, 0.5f, 14f), floorMaterial);
        AddWall(new Vector3(25f, 0f, 7f), 14f, false);   // north
        AddWall(new Vector3(25f, 0f, -7f), 14f, false);  // south
        AddWallWithDoor(new Vector3(32f, 0f, 0f), 14f, true); // east with doorway (step 13)
        AddGate(new Vector3(32f, 1.5f, 0f)); // Step 13 (additive): second locked gate.
        AddWallWithDoor(new Vector3(18f, 0f, 0f), 14f, true); // west with doorway
        AddGate(new Vector3(18f, 1.5f, 0f)); // Step 11 (additive): locked gate.

        // Pillars for cover.
        AddBox("PillarA1", new Vector3(-5f, wallHeight / 2f, -5f), new Vector3(1f, wallHeight, 1f), wallMaterial);
        AddBox("PillarA2", new Vector3(-5f, wallHeight / 2f, 5f), new Vector3(1f, wallHeight, 1f), wallMaterial);
        AddBox("PillarB1", new Vector3(25f, wallHeight / 2f, 3f), new Vector3(1f, wallHeight, 1f), wallMaterial);

        // Corridor 2: x from 32 to 40, 4 wide (step 13).
        AddBox("FloorCorridor2", new Vector3(36f, -0.25f, 0f), new Vector3(8f, 0.5f, 4f), floorMaterial);
        AddBox("WallCorridor2N", new Vector3(36f, wallHeight / 2f, 2.5f), new Vector3(8f, wallHeight, wallThickness), wallMaterial);
        AddBox("WallCorridor2S", new Vector3(36f, wallHeight / 2f, -2.5f), new Vector3(8f, wallHeight, wallThickness), wallMaterial);

        // Hall C: 12x12 centered on x=46, doorway on the west side (step 13).
        AddBox("FloorC", new Vector3(46f, -0.25f, 0f), new Vector3(12f, 0.5f, 12f), floorMaterial);
        AddWall(new Vector3(46f, 0f, 6f), 12f, false);
        AddWall(new Vector3(46f, 0f, -6f), 12f, false);
        AddWall(new Vector3(52f, 0f, 0f), 12f, true);
        AddWallWithDoor(new Vector3(40f, 0f, 0f), 12f, true);
        HallCCenter = new Vector3(46f, 1f, 0f);

        // Warm lights per hall.
        AddLight(new Vector3(0f, 2.5f, 0f));
        AddLight(new Vector3(25f, 2.5f, 0f));
        AddLight(new Vector3(46f, 2.5f, 0f));
    }

    // Full wall of given length. If alongX, the wall runs north-south (faces east/west).
    private void AddWall(Vector3 center, float length, bool alongX)
    {
        Vector3 size = alongX
            ? new Vector3(wallThickness, wallHeight, length)
            : new Vector3(length, wallHeight, wallThickness);
        AddBox("Wall", new Vector3(center.x, wallHeight / 2f, center.z), size, wallMaterial);
    }

    // Wall with a 4-wide doorway gap in the middle.
    private void AddWallWithDoor(Vector3 center, float length, bool alongX)
    {
        float seg = (length - 4f) / 2f;
        if (alongX)
        {
            AddBox("WallSeg", new Vector3(center.x, wallHeight / 2f, center.z + 2f + seg / 2f), new Vector3(wallThickness, wallHeight, seg), wallMaterial);
            AddBox("WallSeg", new Vector3(center.x, wallHeight / 2f, center.z - 2f - seg / 2f), new Vector3(wallThickness, wallHeight, seg), wallMaterial);
        }
        else
        {
            AddBox("WallSeg", new Vector3(center.x + 2f + seg / 2f, wallHeight / 2f, center.z), new Vector3(seg, wallHeight, wallThickness), wallMaterial);
            AddBox("WallSeg", new Vector3(center.x - 2f - seg / 2f, wallHeight / 2f, center.z), new Vector3(seg, wallHeight, wallThickness), wallMaterial);
        }
    }

    private void AddBox(string name, Vector3 center, Vector3 size, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().material = material;
    }

    // Step 11 (additive): solid gate plugging a 4-wide doorway; sinks on unlock.
    private void AddGate(Vector3 center)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Gate";
        go.transform.SetParent(transform, false);
        go.transform.position = center;
        go.transform.localScale = new Vector3(1f, 3f, 4f);
        go.GetComponent<Renderer>().material = MakeMaterial(new Color(0.2f, 0.2f, 0.24f));
        go.AddComponent<Gate>();
    }

    private void AddLight(Vector3 position)
    {
        GameObject go = new GameObject("HallLight");
        go.transform.SetParent(transform, false);
        go.transform.position = position;
        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.8f, 0.6f);
        light.intensity = 20f;
        light.range = 16f;
    }

    private static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader);
        material.color = color;
        return material;
    }
}
