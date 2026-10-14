using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(-50)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const float BaseSpeed = 8f;
    const string HighScoreKey = "DinoRushHighScore";

    public bool IsPlaying { get; private set; }
    public bool InMenu { get; private set; } = true;
    public int HighScore => highScore;
    public bool HasShield { get; private set; }
    public bool JumpLocked { get; private set; }
    public bool IsInvulnerable => invulnerableTimer > 0f;

    public float SpeedMultiplier => Mathf.Lerp(1f, 2.15f, Mathf.Clamp01(score / 900f));
    public float CurrentSpeed => IsPlaying ? BaseSpeed * SpeedMultiplier : 0f;

    float score;
    int highScore;
    float invulnerableTimer;
    float scorePunch;
    float shakeTime;
    float shakeAmount;
    bool canRestart;
    bool restarting;
    int nextMilestone;
    Coroutine bannerRoutine;

    readonly int[] milestones = { 200, 500, 900 };
    readonly string[] milestoneMessages = { "¡MÁS RÁPIDO!", "¡A TODO O NADA!", "¡IMPARABLE!" };

    Font font;
    Camera cam;
    Vector3 cameraOrigin;
    Color dayColor;
    Color sunsetColor;
    Color duskColor;

    Canvas hudCanvas;
    MainMenu mainMenu;
    Text scoreValueText;
    Text recordValueText;
    Text shieldText;
    Text bannerText;
    CanvasGroup hintGroup;
    Image flashImage;
    GameObject gameOverCanvas;
    Text gameOverHeadline;
    Text gameOverScore;
    Text gameOverRecord;
    RectTransform gameOverPanel;
    Transform popupRoot;
    Transform[] clouds;

    static readonly Color Ink = new Color(0.2f, 0.16f, 0.28f, 1f);
    static readonly Color Muted = new Color(0.48f, 0.48f, 0.48f, 1f);
    static readonly Color Gold = new Color(1f, 0.8f, 0.18f, 1f);
    static readonly Color Bone = new Color(0.96f, 0.93f, 0.84f, 1f);
    static readonly Color Cyan = new Color(0.35f, 0.9f, 1f, 1f);

    public static void Ensure()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        font = PixelFont.Get();

        cam = Camera.main;
        if (cam != null)
        {
            cameraOrigin = cam.transform.position;
            dayColor = new Color(0.80f, 0.90f, 0.98f);
            sunsetColor = new Color(1f, 0.78f, 0.55f);
            duskColor = new Color(0.62f, 0.74f, 0.94f);
        }

        popupRoot = new GameObject("Popups").transform;
        BuildHud();
        if (hudCanvas != null)
            hudCanvas.gameObject.SetActive(false);
        BuildGameOver();
        EnsureEventSystem();
        SpawnClouds();
        GameSprites.Star.ToString();
        RefreshHud();
        mainMenu = gameObject.AddComponent<MainMenu>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        JumpLocked = false;

        if (IsPlaying)
        {
            score += 10f * SpeedMultiplier * Time.deltaTime;
            CheckMilestones();
        }

        if (invulnerableTimer > 0f)
            invulnerableTimer -= Time.deltaTime;

        if (scorePunch > 0f)
            scorePunch = Mathf.MoveTowards(scorePunch, 0f, Time.deltaTime * 3.2f);

        UpdateSky();
        UpdateShake();
        MoveClouds();
        RefreshHud();

        if (InMenu && Input.GetKeyDown(KeyCode.Space))
        {
            StartRun();
            return;
        }

        if (canRestart && !restarting && Input.GetKeyDown(KeyCode.Space))
        {
            JumpLocked = true;
            Restart();
        }
    }

    public void StartRun()
    {
        if (!InMenu)
            return;

        InMenu = false;
        JumpLocked = true;
        IsPlaying = true;
        if (hudCanvas != null)
            hudCanvas.gameObject.SetActive(true);
        if (mainMenu != null)
            mainMenu.Close();
        StartCoroutine(FadeHint());
    }

    public void AddBonus(int points, Vector3 worldPosition, string label)
    {
        if (!IsPlaying)
            return;

        score += points;
        scorePunch = 1f;
        SpawnPopup(label, worldPosition, Gold);
    }

    public void CollectStar(Vector3 worldPosition)
    {
        AddBonus(100, worldPosition, "+100");
    }

    public void CollectShield(Vector3 worldPosition)
    {
        if (!IsPlaying)
            return;

        if (HasShield)
        {
            AddBonus(50, worldPosition, "+50");
            return;
        }

        HasShield = true;
        SpawnPopup("ESCUDO", worldPosition, Cyan);
        RefreshShieldLabel();
    }

    public bool TryConsumeHit(Vector3 worldPosition)
    {
        if (!IsPlaying)
            return false;
        if (invulnerableTimer > 0f)
            return true;
        if (!HasShield)
            return false;

        HasShield = false;
        invulnerableTimer = 0.5f;
        RefreshShieldLabel();
        SpawnPopup("¡BLOQUEADO!", worldPosition, Cyan);
        Shake(0.12f, 0.16f);
        return true;
    }

    public void GameOver()
    {
        if (!IsPlaying)
            return;

        IsPlaying = false;
        HasShield = false;
        RefreshShieldLabel();

        int finalScore = Mathf.FloorToInt(score);
        bool isRecord = highScore > 0 && finalScore > highScore;
        if (finalScore > highScore)
        {
            highScore = finalScore;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        RefreshHud();
        StartCoroutine(GameOverSequence(finalScore, isRecord));
    }

    public void Restart()
    {
        if (restarting || !canRestart)
            return;

        restarting = true;
        JumpLocked = true;
        StopAllCoroutines();

        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        for (int i = 0; i < obstacles.Length; i++)
        {
            Collider2D collider = obstacles[i].GetComponent<Collider2D>();
            if (collider != null)
                collider.enabled = false;
            Destroy(obstacles[i]);
        }

        Pickup[] pickups = FindObjectsOfType<Pickup>();
        for (int i = 0; i < pickups.Length; i++)
            Destroy(pickups[i].gameObject);

        for (int i = popupRoot.childCount - 1; i >= 0; i--)
            Destroy(popupRoot.GetChild(i).gameObject);

        score = 0f;
        scorePunch = 0f;
        nextMilestone = 0;
        invulnerableTimer = 0f;
        HasShield = false;
        canRestart = false;
        shakeTime = 0f;

        if (cam != null)
            cam.transform.position = cameraOrigin;
        if (flashImage != null)
            flashImage.color = new Color(1f, 1f, 1f, 0f);
        if (bannerText != null)
            bannerText.text = "";
        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(false);
        if (hintGroup != null)
            hintGroup.alpha = 1f;

        RefreshShieldLabel();
        RefreshHud();

        Dinosaur dinosaur = FindObjectOfType<Dinosaur>();
        if (dinosaur != null)
            dinosaur.Revive();

        IsPlaying = true;
        restarting = false;
        StartCoroutine(FadeHint());
    }

    IEnumerator GameOverSequence(int finalScore, bool isRecord)
    {
        Shake(0.2f, 0.32f);
        float elapsed = 0f;
        while (elapsed < 0.62f)
        {
            elapsed += Time.unscaledDeltaTime;
            float flash = elapsed < 0.08f ? elapsed / 0.08f : 1f - ((elapsed - 0.08f) / 0.4f);
            if (flashImage != null)
                flashImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(flash) * 0.8f);
            yield return null;
        }

        if (flashImage != null)
            flashImage.color = new Color(1f, 1f, 1f, 0f);

        gameOverHeadline.text = isRecord ? "¡NUEVO RÉCORD!" : "¡Chocaste!";
        gameOverHeadline.color = isRecord ? Gold : Bone;
        gameOverScore.text = finalScore.ToString("D5");
        gameOverScore.color = isRecord ? Gold : Bone;
        gameOverRecord.text = highScore.ToString("D5");

        gameOverCanvas.SetActive(true);
        canRestart = true;
        gameOverPanel.localScale = Vector3.one * 0.86f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 5f;
            gameOverPanel.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        gameOverPanel.localScale = Vector3.one;
    }

    void CheckMilestones()
    {
        if (nextMilestone >= milestones.Length)
            return;
        if (Mathf.FloorToInt(score) < milestones[nextMilestone])
            return;

        ShowBanner(milestoneMessages[nextMilestone]);
        nextMilestone++;
    }

    void ShowBanner(string message)
    {
        bannerText.text = message;
        if (bannerRoutine != null)
            StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(BannerRoutine());
    }

    IEnumerator BannerRoutine()
    {
        Color color = Gold;
        float t = 0f;
        while (t < 1.15f)
        {
            t += Time.deltaTime;
            float alpha = t < 0.15f ? t / 0.15f : 1f - Mathf.Clamp01((t - 0.55f) / 0.6f);
            color.a = Mathf.Clamp01(alpha);
            bannerText.color = color;
            bannerText.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, Mathf.Clamp01(t / 0.2f));
            yield return null;
        }

        bannerText.text = "";
    }

    IEnumerator FadeHint()
    {
        if (hintGroup == null)
            yield break;

        hintGroup.alpha = 1f;
        yield return new WaitForSeconds(4.2f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            hintGroup.alpha = 1f - t;
            yield return null;
        }

        hintGroup.alpha = 0f;
    }

    void RefreshHud()
    {
        int shownScore = Mathf.FloorToInt(score);
        if (scoreValueText != null)
        {
            scoreValueText.text = shownScore.ToString("D5");
            bool beatingRecord = highScore > 0 && shownScore > highScore;
            scoreValueText.color = beatingRecord ? Gold : Ink;
            float punch = 1f + scorePunch * 0.16f;
            scoreValueText.rectTransform.localScale = new Vector3(punch, punch, 1f);
        }

        if (recordValueText != null)
            recordValueText.text = highScore.ToString("D5");
    }

    void RefreshShieldLabel()
    {
        if (shieldText != null)
            shieldText.gameObject.SetActive(HasShield);
    }

    void UpdateSky()
    {
        if (cam == null)
            return;

        float cycle = Mathf.Repeat(score / 700f, 2f);
        Color sky = cycle < 1f
            ? Color.Lerp(dayColor, sunsetColor, cycle)
            : Color.Lerp(sunsetColor, duskColor, cycle - 1f);
        cam.backgroundColor = sky;
    }

    public void Shake(float amount, float duration)
    {
        shakeAmount = amount;
        shakeTime = duration;
    }

    void UpdateShake()
    {
        if (cam == null)
            return;

        if (shakeTime > 0f)
        {
            shakeTime -= Time.unscaledDeltaTime;
            Vector2 offset = Random.insideUnitCircle * shakeAmount;
            cam.transform.position = new Vector3(cameraOrigin.x + offset.x, cameraOrigin.y + offset.y, cameraOrigin.z);
            if (shakeTime <= 0f)
                cam.transform.position = cameraOrigin;
        }
    }

    void SpawnClouds()
    {
        clouds = new Transform[5];
        for (int i = 0; i < clouds.Length; i++)
        {
            GameObject cloud = new GameObject("Cloud");
            SpriteRenderer renderer = cloud.AddComponent<SpriteRenderer>();
            renderer.sprite = GameSprites.Cloud;
            renderer.color = new Color(1f, 1f, 1f, 0.92f);
            renderer.sortingOrder = -8;
            float scale = Random.Range(0.85f, 1.7f);
            cloud.transform.localScale = new Vector3(scale, scale * Random.Range(0.75f, 1f), 1f);
            cloud.transform.position = new Vector3(Random.Range(-4f, 16f), Random.Range(4.6f, 7.4f), 0f);
            clouds[i] = cloud.transform;
        }
    }

    void MoveClouds()
    {
        if (clouds == null)
            return;

        float speed = IsPlaying ? CurrentSpeed * 0.22f : (InMenu ? BaseSpeed * 0.12f : 0f);
        if (speed <= 0f)
            return;
        for (int i = 0; i < clouds.Length; i++)
        {
            Vector3 position = clouds[i].position;
            position.x -= speed * Time.deltaTime;
            if (position.x < -12f)
            {
                position.x = 16f + Random.Range(0f, 3f);
                position.y = Random.Range(4.6f, 7.4f);
            }

            clouds[i].position = position;
        }
    }

    public void SpawnPopup(string message, Vector3 worldPosition, Color color)
    {
        if (font == null)
            return;

        GameObject go = new GameObject("Popup");
        go.transform.SetParent(popupRoot, true);
        go.transform.position = worldPosition;

        TextMesh mesh = go.AddComponent<TextMesh>();
        mesh.text = message;
        mesh.font = PixelFont.Get();
        mesh.fontSize = 96;
        mesh.characterSize = 0.22f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
        PixelFont.Prepare(message, 96);
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 30;
        if (font != null && font.material != null)
            renderer.material = font.material;

        StartCoroutine(PopupRoutine(mesh));
    }

    IEnumerator PopupRoutine(TextMesh mesh)
    {
        if (mesh == null)
            yield break;

        Vector3 start = mesh.transform.position;
        Color color = mesh.color;
        float t = 0f;
        while (t < 0.75f && mesh != null)
        {
            t += Time.unscaledDeltaTime;
            float k = t / 0.75f;
            mesh.transform.position = start + Vector3.up * (k * 0.85f);
            color.a = 1f - k;
            mesh.color = color;
            yield return null;
        }

        if (mesh != null)
            Destroy(mesh.gameObject);
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    void BuildHud()
    {
        hudCanvas = CreateCanvas("HUDCanvas", 20);
        Canvas canvas = hudCanvas;
        scoreValueText = CreateText(canvas.transform, "00000", 84, Ink, FontStyle.Bold, TextAnchor.UpperRight);
        Place(scoreValueText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -70f), new Vector2(360f, 100f));

        Text scoreLabel = CreateText(canvas.transform, "PUNTOS", 36, Muted, FontStyle.Bold, TextAnchor.UpperRight);
        Place(scoreLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -18f), new Vector2(360f, 48f));

        recordValueText = CreateText(canvas.transform, "00000", 84, Muted, FontStyle.Bold, TextAnchor.UpperRight);
        Place(recordValueText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-420f, -70f), new Vector2(360f, 100f));

        Text recordLabel = CreateText(canvas.transform, "RÉCORD", 36, Muted, FontStyle.Bold, TextAnchor.UpperRight);
        Place(recordLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-420f, -18f), new Vector2(360f, 48f));

        Text title = CreateText(canvas.transform, "DINO RUSH", 56, Ink, FontStyle.Bold, TextAnchor.UpperLeft);
        Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -18f), new Vector2(520f, 70f));

        Text hint = CreateText(canvas.transform, "ESPACIO salta    ·    ESPACIO en el aire: doble salto    ·    juntá estrellas", 32, Muted, FontStyle.Normal, TextAnchor.UpperLeft);
        Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -96f), new Vector2(1200f, 48f));
        hintGroup = hint.gameObject.AddComponent<CanvasGroup>();

        shieldText = CreateText(canvas.transform, "ESCUDO LISTO", 36, Cyan, FontStyle.Bold, TextAnchor.UpperLeft);
        Place(shieldText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -156f), new Vector2(480f, 52f));
        shieldText.gameObject.SetActive(false);

        bannerText = CreateText(canvas.transform, "", 84, Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
        Place(bannerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(1200f, 120f));

        flashImage = CreateImage(canvas.transform, new Color(1f, 1f, 1f, 0f), "Flash");
        Stretch(flashImage.rectTransform);
        flashImage.raycastTarget = false;
    }

    void BuildGameOver()
    {
        Canvas canvas = CreateCanvas("GameOverCanvas", 40);
        gameOverCanvas = canvas.gameObject;

        Image overlay = CreateImage(canvas.transform, new Color(0.05f, 0.03f, 0.1f, 0.72f), "Overlay");
        Stretch(overlay.rectTransform);

        Image panel = CreateImage(canvas.transform, Color.white, "Panel");
        Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 820f));
        PixelFont.Frame(panel);
        gameOverPanel = panel.rectTransform;

        Text title = CreateText(panel.transform, "GAME OVER", 110, new Color(1f, 0.42f, 0.28f, 1f), FontStyle.Normal, TextAnchor.MiddleCenter);
        Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 290f), new Vector2(980f, 140f));
        AddHardShadow(title);

        gameOverHeadline = CreateText(panel.transform, "", 48, Gold, FontStyle.Normal, TextAnchor.MiddleCenter);
        Place(gameOverHeadline.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(960f, 70f));

        Image pointsBox = CreateImage(panel.transform, new Color(0.07f, 0.05f, 0.12f, 1f), "PointsBox");
        Place(pointsBox.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-230f, 50f), new Vector2(380f, 180f));
        Image recordBox = CreateImage(panel.transform, new Color(0.07f, 0.05f, 0.12f, 1f), "RecordBox");
        Place(recordBox.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(230f, 50f), new Vector2(380f, 180f));

        Text pointsLabel = CreateText(panel.transform, "PUNTOS", 36, Gold, FontStyle.Normal, TextAnchor.MiddleCenter);
        Place(pointsLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-230f, 90f), new Vector2(340f, 52f));

        gameOverScore = CreateText(panel.transform, "00000", 72, Bone, FontStyle.Normal, TextAnchor.MiddleCenter);
        Place(gameOverScore.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-230f, 20f), new Vector2(340f, 90f));

        Text recordLabel = CreateText(panel.transform, "RÉCORD", 36, Gold, FontStyle.Normal, TextAnchor.MiddleCenter);
        Place(recordLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(230f, 90f), new Vector2(340f, 52f));

        gameOverRecord = CreateText(panel.transform, "00000", 72, Bone, FontStyle.Normal, TextAnchor.MiddleCenter);
        Place(gameOverRecord.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(230f, 20f), new Vector2(340f, 90f));

        Text help = CreateText(
            panel.transform,
            "ESPACIO salta. En el aire, otra vez: doble salto.\nEstrellas +100    orbe azul: escudo    pasar cerca suma.",
            32,
            Bone,
            FontStyle.Normal,
            TextAnchor.MiddleCenter);
        Place(help.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(980f, 110f));

        Button button = CreateButton(panel.transform, "REINTENTAR", Restart);
        Place(button.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -240f), new Vector2(520f, 100f));

        Text spaceHint = CreateText(panel.transform, "o apretá ESPACIO", 32, Gold, FontStyle.Normal, TextAnchor.MiddleCenter);
        Place(spaceHint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -350f), new Vector2(640f, 52f));

        gameOverCanvas.SetActive(false);
    }

    static void AddHardShadow(Text text)
    {
        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.05f, 0.02f, 0.08f, 1f);
        shadow.effectDistance = new Vector2(4f, -4f);
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
        text.fontStyle = FontStyle.Normal;
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
        Image image = CreateImage(parent, Color.white, "RestartButton");
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

        Text text = CreateText(image.transform, label, 48, new Color(0.12f, 0.07f, 0.16f, 1f), FontStyle.Normal, TextAnchor.MiddleCenter);
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
