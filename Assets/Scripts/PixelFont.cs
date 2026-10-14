using UnityEngine;
using UnityEngine.UI;

// Carga dinosaur-font y la deja nítida, sin el suavizado de una fuente común.
public static class PixelFont
{
    const string Sample = " ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÁÉÍÓÚÜÑáéíóúüñ0123456789+¡!¿?.,:;-_'";

    static Font font;

    public static Font Get()
    {
        if (font != null)
            return font;

        font = Resources.Load<Font>("dinosaur-font");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Prepare(Sample, 32);
        return font;
    }

    public static void Apply(Text text)
    {
        if (text == null)
            return;

        text.font = Get();
        text.fontStyle = FontStyle.Normal;
        Prepare(text.text, text.fontSize);
    }

    public static void Prepare(string characters, int size)
    {
        Font current = Get();
        if (current == null || string.IsNullOrEmpty(characters))
            return;

        current.RequestCharactersInTexture(characters, Mathf.Max(16, size), FontStyle.Normal);
        if (current.material != null && current.material.mainTexture != null)
            current.material.mainTexture.filterMode = FilterMode.Point;
    }

    public static void Frame(Image image)
    {
        if (image == null)
            return;

        image.sprite = GameSprites.Frame;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        Shadow shadow = image.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(8f, -8f);
    }

    public static void ButtonFace(Image image)
    {
        if (image == null)
            return;

        image.sprite = GameSprites.Button;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        Shadow shadow = image.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.05f, 0.03f, 0.08f, 1f);
        shadow.effectDistance = new Vector2(5f, -5f);
    }
}
