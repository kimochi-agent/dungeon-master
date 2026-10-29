using UnityEngine;

// Facade: one component for dungeon mood. Applies exponential fog and kills
// all ambient light, so the player's torch is the only illumination.
// Added once by GameBootstrap.
public class DungeonAtmosphere : MonoBehaviour
{
    private void Awake()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.black; // Pure void: silhouettes impossible.
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.028f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientIntensity = 0f; // Belt and suspenders: no ambient in any mode.
        RenderSettings.reflectionIntensity = 0f; // Metals go fully dark without direct light.
        RenderSettings.skybox = null; // No skybox rendering or skybox-ambient leak.
    }

}
