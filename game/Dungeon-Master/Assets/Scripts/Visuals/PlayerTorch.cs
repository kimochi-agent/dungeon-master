using UnityEngine;

// The torch in the player's hand: a warm point light riding the player plus
// a small visible flame bottom-right of the first-person view. Attached to
// the player by GameBootstrap. Flicker is the shared TorchFlicker.
public class PlayerTorch : MonoBehaviour
{
    private void Awake()
    {
        PrimitiveBuilder.Part(transform, PrimitiveType.Cylinder, "TorchHandle",
            new Vector3(0.35f, 1.1f, 0.5f), new Vector3(0.09f, 0.225f, 0.09f),
            new Color(0.25f, 0.16f, 0.1f));
        PrimitiveBuilder.Part(transform, PrimitiveType.Sphere, "HandFlame",
            new Vector3(0.35f, 1.42f, 0.5f), new Vector3(0.3f, 0.36f, 0.3f),
            new Color(1f, 0.45f, 0.1f));
        PrimitiveBuilder.Part(transform, PrimitiveType.Sphere, "HandFlameCore",
            new Vector3(0.35f, 1.4f, 0.5f), new Vector3(0.15f, 0.2f, 0.15f),
            new Color(1f, 0.85f, 0.4f));

        GameObject glow = new GameObject("HandLight");
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = new Vector3(0.35f, 1.42f, 0.5f);
        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.6f, 0.3f);
        light.intensity = 15f;
        light.range = 12f;
        glow.AddComponent<TorchFlicker>();
    }
}
