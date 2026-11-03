using UnityEngine;

// Tiny component: breathes a torch light's intensity with layered sines so
// flames feel alive. Lives on the same object as its Light.
public class TorchFlicker : MonoBehaviour
{
    private Light flame;
    private float baseIntensity;
    private float seed;

    private void Awake()
    {
        flame = GetComponent<Light>();
        if (flame != null)
            baseIntensity = flame.intensity;
        seed = Random.value * 10f;
    }

    private void Update()
    {
        if (flame == null)
            return;
        float t = Time.time + seed;
        flame.intensity = baseIntensity * (0.88f + 0.08f * Mathf.Sin(t * 13f) + 0.04f * Mathf.Sin(t * 29f));
    }
}
