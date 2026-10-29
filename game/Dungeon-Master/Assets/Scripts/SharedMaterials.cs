using System.Collections.Generic;
using UnityEngine;

// Build-safe material tints. The base is a serialized URP Lit material asset
// (Resources/Materials/TintBase), which forces the shader into builds —
// unlike Shader.Find or builtin defaults, which get stripped when unreferenced.
public static class SharedMaterials
{
    private static readonly Dictionary<Color, Material> cache = new Dictionary<Color, Material>();
    private static Material baseMaterial;
    private static bool lookedUpBase;

    public static Material Tinted(Renderer source, Color color)
    {
        if (!cache.TryGetValue(color, out Material material) || material == null)
        {
            Material template = BaseMaterial() ?? source.sharedMaterial;
            material = new Material(template);
            material.color = color;
            cache[color] = material;
        }
        return material;
    }

    private static Material BaseMaterial()
    {
        if (lookedUpBase)
            return baseMaterial;
        lookedUpBase = true;
        baseMaterial = Resources.Load<Material>("Materials/TintBase");
        if (baseMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
                baseMaterial = new Material(shader);
        }
        return baseMaterial;
    }
}
