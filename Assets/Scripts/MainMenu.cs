using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Menú de presentación. Se arma solo al dar Play y frena la carrera hasta que eligen jugar.
public class MainMenu : MonoBehaviour
{
    static readonly Color Bone = new Color(0.96f, 0.93f, 0.84f, 1f);
    static readonly Color Gold = new Color(1f, 0.8f, 0.18f, 1f);
    static readonly Color Ink = new Color(0.12f, 0.07f, 0.16f, 1f);

    GameObject canvasObject;
    Font font;
    RectTransform titleRect;
    RectTransform buttonRect;
    Image starIcon;
    Image orbIcon;
    Vector2 starBase;
    Vector2 orbBase;
    Text spaceHint;
    bool closed;

    void Awake()
    {
        font = PixelFont.Get();

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
            float pulse = 1f + Mathf.Sin(time * 3f) * 0.03f;
            buttonRect.localScale = new Vector3(pulse, pulse, 1f);
        }

        if (spaceHint != null)
        {
            float blink = Mathf.PingPong(time * 1.6f, 1f) > 0.35f ? 1f : 0.25f;
            Color color = Gold;
            color.a = blink;
            spaceHint.color = color;
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

        Image overlay = CreateImage(canvas.transform, new Color(0.05f, 0.03f, 0.1f, 0.45f), "Overlay");
        Stretch(overlay.rectTransform);

        Image panel = CreateImage(canvas.transform, Color.white, "Panel");
        Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), new Vector2(1100f, 980f));
        PixelFont.Frame(panel);

        Text kicker = CreateText(panel.transform, "LISTO PARA CORRER", 42, Gold, TextAnchor.MiddleCenter);
        Place(kicker.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 400f), new Vector2(980f, 64f));

        Text title = CreateText(panel.transform, "DINO RUSH", 120, Gold, TextAnchor.MiddleCenter);
        Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 290f), new Vector2(860f, 150f));
        titleRect = title.rectTransform;
        AddHardShadow(title);

        starIcon = CreateIcon(panel.transform, GameSprites.Star, new Vector2(-440f, 290f));
        orbIcon = CreateIcon(panel.transform, GameSprites.Orb, new Vector2(440f, 290f));

        Text lead = CreateText(panel.transform, "El desierto no frena. Vos tampoco.", 48, Bone, TextAnchor.MiddleCenter);
        Place(lead.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(1000f, 72f));

        Text story = CreateText(
            panel.transform,
            "Un dinosaurio sale a correr y el suelo no para.\nLos cactus crecen en el camino y los pterodáctilos cruzan el cielo.\nLlegá más lejos que tu propio récord.",
            36,
            Bone,
            TextAnchor.MiddleCenter);
        Place(story.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1000f, 170f));

        Text how = CreateText(panel.transform, "CÓMO SE JUEGA", 40, Gold, TextAnchor.MiddleCenter);
        Place(how.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(980f, 60f));

        Text controls = CreateText(
            panel.transform,
            "ESPACIO salta. En el aire, ESPACIO otra vez: doble salto.\nEstrellas: +100 puntos.    Orbe azul: un escudo.\nPasar muy cerca de un obstáculo también suma.",
            34,
            Bone,
            TextAnchor.MiddleCenter);
        Place(controls.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -190f), new Vector2(1000f, 160f));
        starBase = starIcon.rectTransform.anchoredPosition;
        orbBase = orbIcon.rectTransform.anchoredPosition;

        int record = GameManager.Instance != null ? GameManager.Instance.HighScore : 0;
        string recordLine = record > 0
            ? "TU RÉCORD  " + record.ToString("D5")
            : "TODAVÍA NO HAY RÉCORD";
        Text recordText = CreateText(panel.transform, recordLine, 36, Gold, TextAnchor.MiddleCenter);
        Place(recordText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -300f), new Vector2(980f, 56f));

        Button button = CreateButton(panel.transform, "JUGAR", () =>
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartRun();
        });
        buttonRect = button.GetComponent<RectTransform>();
        Place(buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -390f), new Vector2(460f, 100f));

        spaceHint = CreateText(panel.transform, "APRETÁ ESPACIO", 36, Gold, TextAnchor.MiddleCenter);
        Place(spaceHint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -455f), new Vector2(700f, 56f));
    }

    Image CreateIcon(Transform parent, Sprite sprite, Vector2 position)
    {
        Image image = CreateImage(parent, Color.white, "Icon");
        image.sprite = sprite;
        image.preserveAspect = true;
        Place(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(84f, 84f));
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

    Text CreateText(Transform parent, string content, int size, Color color, TextAnchor alignment)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.lineSpacing = 1.05f;
        PixelFont.Apply(text);
        return text;
    }

    Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        Image image = CreateImage(parent, Color.white, "PlayButton");
        PixelFont.ButtonFace(image);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.92f, 0.55f, 1f);
        colors.pressedColor = new Color(0.75f, 0.58f, 0.1f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.05f;
        button.colors = colors;

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        Text text = CreateText(image.transform, label, 48, Ink, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        return button;
    }

    static void AddHardShadow(Text text)
    {
        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.05f, 0.02f, 0.08f, 1f);
        shadow.effectDistance = new Vector2(4f, -4f);
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
