using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Menú de presentación. Se arma solo al dar Play y frena la carrera hasta que eligen jugar.
public class MainMenu : MonoBehaviour
{
    static readonly Color Ink = new Color(0.325f, 0.325f, 0.325f, 1f);
    static readonly Color Muted = new Color(0.48f, 0.48f, 0.48f, 1f);
    static readonly Color Gold = new Color(0.86f, 0.62f, 0.08f, 1f);

    GameObject canvasObject;
    Font font;
    RectTransform titleRect;
    RectTransform buttonRect;
    Image starIcon;
    Image orbIcon;
    Vector2 starBase;
    Vector2 orbBase;
    bool closed;

    void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Font.CreateDynamicFontFromOSFont("Arial", 32);

        Build();
        StartCoroutine(PlayIntro());
    }

    void Update()
    {
        if (closed)
            return;

        float time = Time.unscaledTime;
        if (starIcon != null)
        {
            starIcon.rectTransform.anchoredPosition = starBase + new Vector2(0f, Mathf.Sin(time * 2.2f) * 10f);
            starIcon.rectTransform.Rotate(0f, 0f, -24f * Time.unscaledDeltaTime);
        }

        if (orbIcon != null)
        {
            orbIcon.rectTransform.anchoredPosition = orbBase + new Vector2(0f, Mathf.Sin(time * 2.2f + 1.4f) * 10f);
            orbIcon.rectTransform.Rotate(0f, 0f, 18f * Time.unscaledDeltaTime);
        }

        if (buttonRect != null)
        {
            float pulse = 1f + Mathf.Sin(time * 3f) * 0.025f;
            buttonRect.localScale = new Vector3(pulse, pulse, 1f);
        }
    }

    public void Close()
    {
        if (closed)
            return;

        closed = true;
        StopAllCoroutines();
        if (canvasObject != null)
            canvasObject.SetActive(false);
    }

    IEnumerator PlayIntro()
    {
        if (titleRect != null)
            titleRect.localScale = Vector3.one * 0.9f;

        yield return new WaitForSecondsRealtime(0.08f);
        float t = 0f;
        while (t < 1f && titleRect != null)
        {
            t += Time.unscaledDeltaTime * 2.4f;
            float k = Mathf.SmoothStep(0f, 1f, t);
            titleRect.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, k);
            yield return null;
        }

        if (titleRect != null)
            titleRect.localScale = Vector3.one;
    }

    void Build()
    {
        Canvas canvas = CreateCanvas("MainMenuCanvas", 80);
        canvasObject = canvas.gameObject;

        Image overlay = CreateImage(canvas.transform, new Color(0.10f, 0.16f, 0.24f, 0.38f), "Overlay");
        Stretch(overlay.rectTransform);

        Image border = CreateImage(canvas.transform, Ink, "Border");
        Place(border.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120f, 8f), new Vector2(836f, 688f));

        Image panel = CreateImage(canvas.transform, Color.white, "Panel");
        Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120f, 8f), new Vector2(820f, 672f));

        Image stripe = CreateImage(panel.transform, Ink, "Stripe");
        Place(stripe.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(820f, 14f));

        Text kicker = CreateText(panel.transform, "PRESENTACIÓN", 18, Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
        Place(kicker.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 286f), new Vector2(700f, 28f));

        Text title = CreateText(panel.transform, "DINO RUSH", 68, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
        Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 214f), new Vector2(560f, 80f));
        titleRect = title.rectTransform;

        starIcon = CreateIcon(panel.transform, GameSprites.Star, new Vector2(-330f, 214f));
        orbIcon = CreateIcon(panel.transform, GameSprites.Orb, new Vector2(330f, 214f));

        Text lead = CreateText(panel.transform, "El desierto no frena. Vos tampoco.", 24, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
        Place(lead.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(720f, 36f));

        Text story = CreateText(
            panel.transform,
            "Un dinosaurio sale a correr y el suelo no para.\nLos cactus crecen en el camino y los pterodáctilos cruzan el cielo.\nLlegá más lejos que tu propio récord.",
            22,
            Muted,
            FontStyle.Normal,
            TextAnchor.MiddleCenter);
        Place(story.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 48f), new Vector2(720f, 100f));

        Image divider = CreateImage(panel.transform, new Color(0.85f, 0.85f, 0.85f, 1f), "Divider");
        Place(divider.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -16f), new Vector2(640f, 2f));

        Text how = CreateText(panel.transform, "CÓMO SE JUEGA", 18, Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
        Place(how.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -48f), new Vector2(700f, 28f));

        Text controls = CreateText(
            panel.transform,
            "ESPACIO salta. En el aire, ESPACIO otra vez: doble salto.\nEstrellas: +100 puntos.    Orbe azul: un escudo.\nPasar muy cerca de un obstáculo también suma.",
            20,
            Muted,
            FontStyle.Normal,
            TextAnchor.MiddleCenter);
        Place(controls.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -118f), new Vector2(720f, 90f));
        starBase = starIcon.rectTransform.anchoredPosition;
        orbBase = orbIcon.rectTransform.anchoredPosition;

        int record = GameManager.Instance != null ? GameManager.Instance.HighScore : 0;
        string recordLine = record > 0
            ? "TU RÉCORD  " + record.ToString("D5")
            : "TODAVÍA NO HAY RÉCORD";
        Text recordText = CreateText(panel.transform, recordLine, 18, Muted, FontStyle.Bold, TextAnchor.MiddleCenter);
        Place(recordText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -176f), new Vector2(700f, 28f));

        Button button = CreateButton(panel.transform, "JUGAR", () =>
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartRun();
        });
        buttonRect = button.GetComponent<RectTransform>();
        Place(buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -240f), new Vector2(320f, 64f));

        Text spaceHint = CreateText(panel.transform, "o apretá ESPACIO para salir a correr", 18, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        Place(spaceHint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -300f), new Vector2(520f, 28f));
    }

    Image CreateIcon(Transform parent, Sprite sprite, Vector2 position)
    {
        Image image = CreateImage(parent, Color.white, "Icon");
        image.sprite = sprite;
        image.preserveAspect = true;
        Place(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(54f, 54f));
        return image;
    }

    Canvas CreateCanvas(string name, int sortOrder)
    {
        GameObject go = new GameObject(name);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    Image CreateImage(Transform parent, Color color, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    Text CreateText(Transform parent, string content, int size, Color color, FontStyle style, TextAnchor alignment)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        Image image = CreateImage(parent, Color.white, "PlayButton");
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        ColorBlock colors = button.colors;
        colors.normalColor = Ink;
        colors.highlightedColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        colors.pressedColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        colors.selectedColor = Ink;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        Text text = CreateText(image.transform, label, 28, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        return button;
    }

    static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(anchorMin.x, anchorMax.y);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
