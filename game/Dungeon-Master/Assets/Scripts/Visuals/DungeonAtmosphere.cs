using UnityEngine;

// Facade: one component for dungeon mood. Applies exponential fog and plants
// flickering torches (pole + two-tone flame + point light) at curated spots.
// Added once by GameBootstrap. No sun: the dungeon is fully indoors.
public class DungeonAtmosphere : MonoBehaviour
{
    private static readonly Vector3[] TorchSpots = new Vector3[]
    {
        new Vector3(-8f, 0f, 8f),   // Hall A corner.
        new Vector3(12f, 0f, 1.2f), // Corridor wall.
        new Vector3(28f, 0f, -5f),  // Hall B, near the east gate.
        new Vector3(44f, 0f, 4f),   // Hall C, near victory.
    };

    private void Awake()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.05f, 0.05f, 0.08f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.028f;
        foreach (Vector3 spot in TorchSpots)
            BuildTorch(spot);
    }

    private void BuildTorch(Vector3 ground)
    {
        GameObject torch = new GameObject("Torch");
        torch.transform.SetParent(transform, false);
        torch.transform.position = ground;

        PrimitiveBuilder.Part(torch.transform, PrimitiveType.Cylinder, "Pole", new Vector3(0f, 0.85f, 0f),
            new Vector3(0.12f, 0.85f, 0.12f), new Color(0.25f, 0.16f, 0.1f));
        PrimitiveBuilder.Part(torch.transform, PrimitiveType.Sphere, "Flame", new Vector3(0f, 1.8f, 0f),
            new Vector3(0.36f, 0.44f, 0.36f), new Color(1f, 0.45f, 0.1f));
        PrimitiveBuilder.Part(torch.transform, PrimitiveType.Sphere, "Core", new Vector3(0f, 1.76f, 0f),
            new Vector3(0.18f, 0.24f, 0.18f), new Color(1f, 0.85f, 0.4f));

        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(torch.transform, false);
        glow.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.55f, 0.25f);
        light.intensity = 12f;
        light.range = 10f;
        glow.AddComponent<TorchFlicker>();
    }
}
