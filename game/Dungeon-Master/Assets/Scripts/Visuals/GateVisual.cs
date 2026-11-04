using UnityEngine;

// Decorator: dresses a gate root (blocking cube + Gate behaviour) as a real
// dungeon door: vertical wooden planks in alternating shades, iron bands with
// rivets, dark edge posts and a knob. Layout derives from the root scale, so
// both door orientations work with no extra parameters. Pure visuals: Gate.cs
// behaviour is untouched.
public static class GateVisual
{
    private static readonly Color WoodLight = new Color(0.45f, 0.29f, 0.15f);
    private static readonly Color WoodDark = new Color(0.36f, 0.23f, 0.12f);
    private static readonly Color WoodEdge = new Color(0.27f, 0.17f, 0.10f);
    private static readonly Color Iron = new Color(0.16f, 0.16f, 0.18f);
    private static readonly Color IronDark = new Color(0.09f, 0.09f, 0.11f);

    private const int PlankCount = 5;

    public static void Attach(GameObject gate)
    {
        Vector3 dims = gate.transform.localScale;
        bool wideAlongZ = dims.z > dims.x; // alongX doors span Z.
        float width = wideAlongZ ? dims.z : dims.x;
        float height = dims.y;
        float thick = wideAlongZ ? dims.x : dims.z;

        // Unscaled frame so children use world units despite the root scale.
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(gate.transform, false);
        visual.transform.localScale = new Vector3(1f / dims.x, 1f / dims.y, 1f / dims.z);

        float plankW = width / PlankCount;
        for (int i = 0; i < PlankCount; i++)
        {
            float u = -width / 2f + plankW * (i + 0.5f);
            Color wood = i % 2 == 0 ? WoodLight : WoodDark;
            PrimitiveBuilder.Part(visual.transform, PrimitiveType.Cube, "Plank" + i,
                Frame(wideAlongZ, u, 0f, 0f),
                Frame(wideAlongZ, plankW * 0.96f, height * 0.98f, thick + 0.08f), wood);
        }
        foreach (float v in new[] { -height * 0.25f, height * 0.25f })
        {
            PrimitiveBuilder.Part(visual.transform, PrimitiveType.Cube, "Band",
                Frame(wideAlongZ, 0f, v, 0f),
                Frame(wideAlongZ, width * 0.99f, 0.22f, thick + 0.16f), Iron);
            for (int k = -1; k <= 1; k++)
                foreach (float s in new[] { -1f, 1f })
                    PrimitiveBuilder.Part(visual.transform, PrimitiveType.Sphere, "Rivet",
                        Frame(wideAlongZ, k * width * 0.3f, v, s * (thick / 2f + 0.08f)),
                        new Vector3(0.12f, 0.12f, 0.12f), IronDark);
        }
        foreach (float s in new[] { -1f, 1f })
            PrimitiveBuilder.Part(visual.transform, PrimitiveType.Cube, "Post",
                Frame(wideAlongZ, s * (width / 2f - 0.09f), 0f, 0f),
                Frame(wideAlongZ, 0.18f, height, thick + 0.2f), WoodEdge);
        PrimitiveBuilder.Part(visual.transform, PrimitiveType.Cube, "KnobPlate",
            Frame(wideAlongZ, width * 0.28f, 0f, thick / 2f + 0.06f),
            Frame(wideAlongZ, 0.2f, 0.3f, 0.08f), IronDark);
        PrimitiveBuilder.Part(visual.transform, PrimitiveType.Sphere, "Knob",
            Frame(wideAlongZ, width * 0.28f, 0f, thick / 2f + 0.16f),
            new Vector3(0.14f, 0.14f, 0.14f), Iron);
    }

    // Maps (u = along door width, v = up, w = out of door face) to local axes.
    private static Vector3 Frame(bool wideAlongZ, float u, float v, float w)
    {
        return wideAlongZ ? new Vector3(w, v, u) : new Vector3(u, v, w);
    }
}
