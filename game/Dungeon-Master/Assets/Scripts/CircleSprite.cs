using UnityEngine;

// Generates a soft-edged white circle sprite in code (no art assets needed).
// Used by the touch controls: joystick, knobs, round buttons.
public static class CircleSprite
{
    private static Sprite cached;

    public static Sprite Get(int size = 128)
    {
        if (cached != null)
            return cached;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / radius;
                float alpha = Mathf.Clamp01((1f - d) * 4f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        cached = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cached;
    }
}
