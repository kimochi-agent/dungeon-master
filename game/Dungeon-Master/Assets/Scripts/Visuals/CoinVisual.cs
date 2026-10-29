using UnityEngine;

// Factory: builds a minted-look coin (gold face + darker rim, metallic URP
// Lit finish) mounted upright so the collector's Y-spin flashes its faces.
// Pure visuals: CollectiblePlacer owns positioning, parenting and behavior.
public static class CoinVisual
{
    private static readonly Color FaceGold = new Color(1f, 0.78f, 0.2f);
    private static readonly Color RimGold = new Color(0.72f, 0.5f, 0.12f);

    public static GameObject Create()
    {
        GameObject go = new GameObject("Coin");
        GameObject holder = new GameObject("Spin");
        holder.transform.SetParent(go.transform, false);
        holder.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        AddDisc(holder.transform, new Vector3(0.6f, 0.08f, 0.6f), FaceGold);
        AddDisc(holder.transform, new Vector3(0.68f, 0.05f, 0.68f), RimGold);
        return go;
    }

    private static void AddDisc(Transform parent, Vector3 scale, Color color)
    {
        GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(mesh.GetComponent<Collider>());
        mesh.transform.SetParent(parent, false);
        mesh.transform.localScale = scale;
        Renderer renderer = mesh.GetComponent<Renderer>();
        renderer.sharedMaterial = SharedMaterials.TintedMetallic(renderer, color, 0.9f, 0.7f);
    }
}
