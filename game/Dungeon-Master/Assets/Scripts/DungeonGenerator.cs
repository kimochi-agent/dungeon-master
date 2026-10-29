using UnityEngine;

// Step 1: builds a small explorable dungeon (two halls joined by a corridor)
// out of primitives. Builds itself on Awake when empty.
public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private float wallHeight = 3f;
    [SerializeField] private float wallThickness = 1f;

    public Vector3 SpawnPoint = new Vector3(-6f, 1f, 0f);
    public Vector3 HallCCenter { get; private set; } // Step 13 (additive).

    private Color floorColor;
    private Color wallColor;
    private Color ceilingColor;

    private void Awake()
    {
        if (transform.childCount == 0)
            Build();
    }

    public void Build()
    {
        floorColor = new Color(0.32f, 0.30f, 0.34f);
        wallColor = new Color(0.45f, 0.32f, 0.24f);
        ceilingColor = new Color(0.24f, 0.19f, 0.2f);

        // Hall A: 20x20 centered on origin, doorway on the east side.
        AddBox("FloorA", new Vector3(0f, -0.25f, 0f), new Vector3(20f, 0.5f, 20f), floorColor);
        AddWall(new Vector3(0f, 0f, 10f), 20f, false);   // north
        AddWall(new Vector3(0f, 0f, -10f), 20f, false);  // south
        AddWall(new Vector3(-10f, 0f, 0f), 20f, true);   // west
        AddWallWithDoor(new Vector3(10f, 0f, 0f), 20f, true); // east with doorway

        // Corridor: x from 10 to 18, 4 wide.
        AddBox("FloorCorridor", new Vector3(14f, -0.25f, 0f), new Vector3(8f, 0.5f, 4f), floorColor);
        AddBox("WallCorridorN", new Vector3(14f, wallHeight / 2f, 2.5f), new Vector3(8f, wallHeight, wallThickness), wallColor);
        AddBox("WallCorridorS", new Vector3(14f, wallHeight / 2f, -2.5f), new Vector3(8f, wallHeight, wallThickness), wallColor);

        // Hall B: 14x14 centered on x=25, doorway on the west side.
        AddBox("FloorB", new Vector3(25f, -0.25f, 0f), new Vector3(14f, 0.5f, 14f), floorColor);
        AddWall(new Vector3(25f, 0f, 7f), 14f, false);   // north
        AddWall(new Vector3(25f, 0f, -7f), 14f, false);  // south
        AddWallWithDoor(new Vector3(32f, 0f, 0f), 14f, true); // east with doorway (step 13)
        AddGate(new Vector3(32f, 1.5f, 0f)); // Step 13 (additive): second locked gate.
        AddWallWithDoor(new Vector3(18f, 0f, 0f), 14f, true); // west with doorway
        AddGate(new Vector3(18f, 1.5f, 0f)); // Step 11 (additive): locked gate.

        // Pillars for cover.
        AddBox("PillarA1", new Vector3(-5f, wallHeight / 2f, -5f), new Vector3(1f, wallHeight, 1f), wallColor);
        AddBox("PillarA2", new Vector3(-5f, wallHeight / 2f, 5f), new Vector3(1f, wallHeight, 1f), wallColor);
        AddBox("PillarB1", new Vector3(25f, wallHeight / 2f, 3f), new Vector3(1f, wallHeight, 1f), wallColor);

        // Corridor 2: x from 32 to 40, 4 wide (step 13).
        AddBox("FloorCorridor2", new Vector3(36f, -0.25f, 0f), new Vector3(8f, 0.5f, 4f), floorColor);
        AddBox("WallCorridor2N", new Vector3(36f, wallHeight / 2f, 2.5f), new Vector3(8f, wallHeight, wallThickness), wallColor);
        AddBox("WallCorridor2S", new Vector3(36f, wallHeight / 2f, -2.5f), new Vector3(8f, wallHeight, wallThickness), wallColor);

        // Hall C: 12x12 centered on x=46, doorway on the west side (step 13).
        AddBox("FloorC", new Vector3(46f, -0.25f, 0f), new Vector3(12f, 0.5f, 12f), floorColor);
        AddWall(new Vector3(46f, 0f, 6f), 12f, false);
        AddWall(new Vector3(46f, 0f, -6f), 12f, false);
        AddWall(new Vector3(52f, 0f, 0f), 12f, true);
        AddWallWithDoor(new Vector3(40f, 0f, 0f), 12f, true);
        HallCCenter = new Vector3(46f, 1f, 0f);

        // Ceilings: close the rooms (slabs flush with wall tops at y=3).
        AddBox("CeilingA", new Vector3(0f, 3.25f, 0f), new Vector3(20f, 0.5f, 20f), ceilingColor);
        AddBox("CeilingCorridor", new Vector3(14f, 3.25f, 0f), new Vector3(8f, 0.5f, 4f), ceilingColor);
        AddBox("CeilingB", new Vector3(25f, 3.25f, 0f), new Vector3(14f, 0.5f, 14f), ceilingColor);
        AddBox("CeilingCorridor2", new Vector3(36f, 3.25f, 0f), new Vector3(8f, 0.5f, 4f), ceilingColor);
        AddBox("CeilingC", new Vector3(46f, 3.25f, 0f), new Vector3(12f, 0.5f, 12f), ceilingColor);

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
        AddBox("Wall", new Vector3(center.x, wallHeight / 2f, center.z), size, wallColor);
    }

    // Wall with a 4-wide doorway gap in the middle.
    private void AddWallWithDoor(Vector3 center, float length, bool alongX)
    {
        float seg = (length - 4f) / 2f;
        if (alongX)
        {
            AddBox("WallSeg", new Vector3(center.x, wallHeight / 2f, center.z + 2f + seg / 2f), new Vector3(wallThickness, wallHeight, seg), wallColor);
            AddBox("WallSeg", new Vector3(center.x, wallHeight / 2f, center.z - 2f - seg / 2f), new Vector3(wallThickness, wallHeight, seg), wallColor);
        }
        else
        {
            AddBox("WallSeg", new Vector3(center.x + 2f + seg / 2f, wallHeight / 2f, center.z), new Vector3(seg, wallHeight, wallThickness), wallColor);
            AddBox("WallSeg", new Vector3(center.x - 2f - seg / 2f, wallHeight / 2f, center.z), new Vector3(seg, wallHeight, wallThickness), wallColor);
        }
    }

    private void AddBox(string name, Vector3 center, Vector3 size, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.position = center;
        go.transform.localScale = size;
        Renderer renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = SharedMaterials.Tinted(renderer, color);
    }

    // Step 11 (additive): solid gate plugging a 4-wide doorway; sinks on unlock.
    private void AddGate(Vector3 center)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Gate";
        go.transform.SetParent(transform, false);
        go.transform.position = center;
        go.transform.localScale = new Vector3(1f, 3f, 4f);
        Renderer renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = SharedMaterials.Tinted(renderer, new Color(0.2f, 0.2f, 0.24f));
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
}
