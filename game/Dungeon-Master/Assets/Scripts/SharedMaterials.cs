using System.Collections.Generic;
using UnityEngine;

// Build-safe material tints. Clones the primitive's own default material
// (always included in builds) instead of Shader.Find (stripped from builds
// when nothing references the shader), and shares one instance per color.
public static class SharedMaterials
{
    private static readonly Dictionary<Color, Material> cache = new Dictionary<Color, Material>();

    public static Material Tinted(Renderer source, Color color)
    {
        if (!cache.TryGetValue(color, out Material material) || material == null)
        {
            material = new Material(source.sharedMaterial);
            material.color = color;
            cache[color] = material;
        }
        return material;
    }
}
