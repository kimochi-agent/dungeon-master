using System.Collections.Generic;
using UnityEngine;

// Step 1: builds the dungeon out of primitives from the level config
// (rooms, corridors, doors). Builds itself on Awake when empty.
public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private float wallHeight = 3f;
    [SerializeField] private float wallThickness = 1f;

    public Vector3 SpawnPoint { get; private set; }
    public Vector3 ExitPoint { get; private set; }

    private LevelData layout;
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
        layout = GameConfig.Current;
        floorColor = new Color(0.32f, 0.30f, 0.34f);
        wallColor = new Color(0.45f, 0.32f, 0.24f);
        ceilingColor = new Color(0.24f, 0.19f, 0.2f);
        SpawnPoint = layout.playerSpawn;
        ExitPoint = layout.exitPoint;

        foreach (RoomSpec room in layout.rooms)
            BuildRoom(room);
        foreach (CorridorSpec corridor in layout.corridors)
            BuildCorridor(corridor);
        foreach (DoorSpec door in layout.doors)
            BuildDoor(door);
        foreach (Vector3 pillar in layout.pillars)
            AddBox("Pillar", new Vector3(pillar.x, wallHeight / 2f, pillar.z), new Vector3(1f, wallHeight, 1f), wallColor);
        // No lights: the player's torch (PlayerTorch) is the only light.
    }

    private void BuildRoom(RoomSpec room)
    {
        float x0 = room.center.x - room.size.x / 2f;
        float x1 = room.center.x + room.size.x / 2f;
        float z0 = room.center.y - room.size.y / 2f;
        float z1 = room.center.y + room.size.y / 2f;
        Vector3 flat = new Vector3(room.center.x, 0f, room.center.y);
        AddBox("Floor", flat + new Vector3(0f, -0.25f, 0f), new Vector3(room.size.x, 0.5f, room.size.y), floorColor);
        AddBox("Ceiling", flat + new Vector3(0f, wallHeight + 0.3f, 0f), new Vector3(room.size.x, 0.5f, room.size.y), ceilingColor);
        EmitSide(z1, false, x0, x1); // North.
        EmitSide(z0, false, x0, x1); // South.
        EmitSide(x1, true, z0, z1);  // East.
        EmitSide(x0, true, z0, z1);  // West.
    }

    private void BuildCorridor(CorridorSpec spec)
    {
        bool horizontal = Mathf.Abs(spec.from.y - spec.to.y) < 0.01f;
        float length = horizontal ? Mathf.Abs(spec.to.x - spec.from.x) : Mathf.Abs(spec.to.y - spec.from.y);
        Vector2 mid = (spec.from + spec.to) / 2f;
        Vector3 center = new Vector3(mid.x, 0f, mid.y);
        Vector3 footprint = horizontal
            ? new Vector3(length, 0.5f, spec.width)
            : new Vector3(spec.width, 0.5f, length);
        AddBox("Floor", center + new Vector3(0f, -0.25f, 0f), footprint, floorColor);
        AddBox("Ceiling", center + new Vector3(0f, wallHeight + 0.3f, 0f), footprint, ceilingColor);
        float off = spec.width / 2f + wallThickness / 2f + 0.05f; // Lip avoids coplanar flicker.
        if (horizontal)
        {
            AddBox("Wall", new Vector3(mid.x, wallHeight / 2f, mid.y + off), new Vector3(length, wallHeight, wallThickness), wallColor);
            AddBox("Wall", new Vector3(mid.x, wallHeight / 2f, mid.y - off), new Vector3(length, wallHeight, wallThickness), wallColor);
        }
        else
        {
            AddBox("Wall", new Vector3(mid.x + off, wallHeight / 2f, mid.y), new Vector3(wallThickness, wallHeight, length), wallColor);
            AddBox("Wall", new Vector3(mid.x - off, wallHeight / 2f, mid.y), new Vector3(wallThickness, wallHeight, length), wallColor);
        }
    }

    private void BuildDoor(DoorSpec door)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Gate";
        go.transform.SetParent(transform, false);
        go.transform.position = door.position;
        go.transform.localScale = door.alongX ? new Vector3(1f, 3f, 3.9f) : new Vector3(3.9f, 3f, 1f);
        Renderer renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = SharedMaterials.Tinted(renderer, new Color(0.2f, 0.2f, 0.24f));
        go.AddComponent<Gate>();
    }

    // One room side: full wall minus merged corridor/door gaps.
    private void EmitSide(float plane, bool alongX, float spanMin, float spanMax)
    {
        float cursor = spanMin;
        foreach (Vector2 gap in GapsOnSide(plane, alongX, spanMin, spanMax))
        {
            EmitSegment(plane, alongX, cursor, gap.x);
            cursor = gap.y;
        }
        EmitSegment(plane, alongX, cursor, spanMax);
    }

    private void EmitSegment(float plane, bool alongX, float a, float b)
    {
        if (b - a < 0.01f)
            return;
        float mid = (a + b) / 2f;
        if (alongX)
            AddBox("WallSeg", new Vector3(plane, wallHeight / 2f, mid), new Vector3(wallThickness, wallHeight, b - a), wallColor);
        else
            AddBox("WallSeg", new Vector3(mid, wallHeight / 2f, plane), new Vector3(b - a, wallHeight, wallThickness), wallColor);
    }

    private List<Vector2> GapsOnSide(float plane, bool alongX, float spanMin, float spanMax)
    {
        List<Vector2> gaps = new List<Vector2>();
        const float eps = 0.6f;
        foreach (CorridorSpec c in layout.corridors)
        {
            bool horizontal = Mathf.Abs(c.from.y - c.to.y) < 0.01f;
            if (horizontal != alongX)
                continue;
            float cross = horizontal ? c.from.y : c.from.x;
            float a = horizontal ? Mathf.Min(c.from.x, c.to.x) : Mathf.Min(c.from.y, c.to.y);
            float b = horizontal ? Mathf.Max(c.from.x, c.to.x) : Mathf.Max(c.from.y, c.to.y);
            if (plane + eps < a || plane - eps > b || cross < spanMin || cross > spanMax)
                continue;
            gaps.Add(new Vector2(Mathf.Max(spanMin, cross - c.width / 2f), Mathf.Min(spanMax, cross + c.width / 2f)));
        }
        foreach (DoorSpec d in layout.doors)
        {
            if (d.alongX != alongX)
                continue;
            float doorPlane = d.alongX ? d.position.x : d.position.z;
            float doorAt = d.alongX ? d.position.z : d.position.x;
            if (Mathf.Abs(doorPlane - plane) > eps || doorAt < spanMin || doorAt > spanMax)
                continue;
            gaps.Add(new Vector2(Mathf.Max(spanMin, doorAt - 2f), Mathf.Min(spanMax, doorAt + 2f)));
        }
        gaps.Sort((p, q) => p.x.CompareTo(q.x));
        List<Vector2> merged = new List<Vector2>();
        foreach (Vector2 g in gaps)
        {
            if (merged.Count > 0 && g.x <= merged[merged.Count - 1].y + 0.01f)
                merged[merged.Count - 1] = new Vector2(merged[merged.Count - 1].x, Mathf.Max(merged[merged.Count - 1].y, g.y));
            else
                merged.Add(g);
        }
        return merged;
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

}
