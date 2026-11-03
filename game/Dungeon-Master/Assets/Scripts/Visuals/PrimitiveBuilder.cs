using UnityEngine;

// Shared micro-builder for decor primitives: mesh + tinted shared material,
// no collider (decor never blocks movement). Used by torches and flames.
public static class PrimitiveBuilder
{
    public static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        Object.Destroy(go.GetComponent<Collider>());
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = SharedMaterials.Tinted(renderer, color);
        return go;
    }
}
