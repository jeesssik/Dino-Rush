using UnityEngine;

// Dibuja estrellas, escudos y nubes en código para no depender de sprites nuevos.
public static class GameSprites
{
    static Sprite star;
    static Sprite orb;
    static Sprite ring;
    static Sprite cloud;
    static Sprite frame;
    static Sprite button;

    public static Sprite Star => star != null ? star : (star = MakeStar());
    public static Sprite Orb => orb != null ? orb : (orb = MakeOrb());
    public static Sprite Ring => ring != null ? ring : (ring = MakeRing());
    public static Sprite Cloud => cloud != null ? cloud : (cloud = MakeCloud());
    public static Sprite Frame => frame != null ? frame : (frame = MakeFrame());
    public static Sprite Button => button != null ? button : (button = MakeButton());

    static Sprite MakeStar()
    {
        const int size = 32;
        Texture2D tex = NewTexture(size);
        float center = (size - 1) * 0.5f;
        float maxRadius = size * 0.47f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) + Mathf.PI * 0.5f;
                float local = Mathf.Repeat(angle / (Mathf.PI * 2f) * 5f, 1f);
                float tip = Mathf.Abs(local - 0.5f) * 2f;
                float radius = Mathf.Lerp(0.40f, 1f, tip) * maxRadius;

                if (dist > radius)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                Color color = dist > radius - 1.6f
                    ? new Color(0.86f, 0.42f, 0.05f, 1f)
                    : new Color(1f, 0.86f, 0.18f, 1f);
                if (dist < 3.2f)
                    color = Color.white;

                tex.SetPixel(x, y, color);
            }
        }

        return Finish(tex, size);
    }

    static Sprite MakeOrb()
    {
        const int size = 28;
        Texture2D tex = NewTexture(size);
        float center = (size - 1) * 0.5f;
        float outer = size * 0.46f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist > outer)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                Color color = dist > outer - 2f
                    ? new Color(0.05f, 0.45f, 0.72f, 1f)
                    : new Color(0.35f, 0.9f, 1f, 1f);
                if (dist < outer * 0.38f)
                    color = new Color(0.85f, 1f, 1f, 1f);

                tex.SetPixel(x, y, color);
            }
        }

        return Finish(tex, size);
    }

    static Sprite MakeRing()
    {
        const int size = 32;
        Texture2D tex = NewTexture(size);
        float center = (size - 1) * 0.5f;
        float outer = size * 0.48f;
        float inner = size * 0.36f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                bool onRing = dist <= outer && dist >= inner;
                tex.SetPixel(x, y, onRing ? new Color(0.25f, 0.9f, 1f, 0.95f) : Color.clear);
            }
        }

        return Finish(tex, size);
    }

    static Sprite MakeCloud()
    {
        const int size = 48;
        Texture2D tex = NewTexture(size);
        Vector2[] puffs =
        {
            new Vector2(16f, 20f),
            new Vector2(26f, 24f),
            new Vector2(34f, 19f),
            new Vector2(24f, 16f)
        };
        float[] radii = { 10f, 12f, 9f, 8f };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = false;
                Vector2 point = new Vector2(x, y);
                for (int i = 0; i < puffs.Length; i++)
                {
                    if (Vector2.Distance(point, puffs[i]) <= radii[i])
                    {
                        inside = true;
                        break;
                    }
                }

                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }

        return Finish(tex, 24f);
    }

    static Sprite MakeFrame()
    {
        const int size = 48;
        Texture2D tex = NewTexture(size);
        Color edge = new Color(0.04f, 0.03f, 0.07f, 1f);
        Color gold = new Color(1f, 0.78f, 0.16f, 1f);
        Color fill = new Color(0.14f, 0.10f, 0.22f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int edgeDistance = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));
                Color color = fill;
                if (edgeDistance <= 1)
                    color = edge;
                else if (edgeDistance <= 3)
                    color = gold;
                else if (edgeDistance == 4)
                    color = edge;

                tex.SetPixel(x, y, color);
            }
        }

        return FinishBorder(tex, 8f);
    }

    static Sprite MakeButton()
    {
        const int size = 32;
        Texture2D tex = NewTexture(size);
        Color edge = new Color(0.08f, 0.05f, 0.02f, 1f);
        Color gold = new Color(1f, 0.78f, 0.12f, 1f);
        Color shine = new Color(1f, 0.92f, 0.45f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int edgeDistance = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));
                Color color = y > size - 8 ? shine : gold;
                if (edgeDistance <= 1)
                    color = edge;
                tex.SetPixel(x, y, color);
            }
        }

        return FinishBorder(tex, 5f);
    }

    static Texture2D NewTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }

    static Sprite Finish(Texture2D tex, float pixelsPerUnit)
    {
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    static Sprite FinishBorder(Texture2D tex, float border)
    {
        tex.Apply();
        return Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));
    }
}
