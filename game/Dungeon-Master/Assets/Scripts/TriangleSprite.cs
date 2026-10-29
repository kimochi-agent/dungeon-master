using UnityEngine;

// Generates a soft-edged upward triangle sprite in code (no art assets).
// Rotate copies for the other three directions.
public static class TriangleSprite
{
    private static Sprite cached;

    public static Sprite Get(int size = 128)
    {
        if (cached != null)
            return cached;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = size / 2f;
        float baseY = 6f;
        float apexY = size - 6f;
        float halfBase = size / 2f - 6f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float alpha = 0f;
                if (py >= baseY && py <= apexY)
                {
                    float halfWidth = (py - baseY) / (apexY - baseY) * halfBase;
                    alpha = Mathf.Clamp01((halfWidth - Mathf.Abs(px - cx)) / 2f);
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        cached = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cached;
    }
}
