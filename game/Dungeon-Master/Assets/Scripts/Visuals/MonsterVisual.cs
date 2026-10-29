using UnityEngine;

// Decorator: attaches cartoon eyes (white + pupil) to a monster root so it
// reads as a creature instead of a capsule. Positions assume the spawner's
// 1.1x capsule. Pure visuals: Monster.cs behavior is untouched.
public static class MonsterVisual
{
    public static void Attach(GameObject monster)
    {
        AddEye(monster.transform, new Vector3(-0.2f, 0.55f, 0.42f));
        AddEye(monster.transform, new Vector3(0.2f, 0.55f, 0.42f));
    }

    private static void AddEye(Transform parent, Vector3 localPos)
    {
        GameObject white = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(white.GetComponent<Collider>());
        white.name = "EyeWhite";
        white.transform.SetParent(parent, false);
        white.transform.localPosition = localPos;
        white.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
        Renderer whiteRenderer = white.GetComponent<Renderer>();
        whiteRenderer.sharedMaterial = SharedMaterials.Tinted(whiteRenderer, Color.white);

        GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(pupil.GetComponent<Collider>());
        pupil.name = "EyePupil";
        pupil.transform.SetParent(parent, false);
        pupil.transform.localPosition = localPos + new Vector3(0f, 0f, 0.1f);
        pupil.transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);
        Renderer pupilRenderer = pupil.GetComponent<Renderer>();
        pupilRenderer.sharedMaterial = SharedMaterials.Tinted(pupilRenderer, new Color(0.05f, 0.05f, 0.05f));
    }
}
