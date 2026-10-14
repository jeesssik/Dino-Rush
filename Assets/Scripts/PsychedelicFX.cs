using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Efectos visuales psicodélicos: wash de color, trails, partículas y speed lines.
public class PsychedelicFX : MonoBehaviour
{
    public static PsychedelicFX Instance { get; private set; }

    const int MaxParticles = 64;
    const int MaxGhosts = 10;
    const int MaxSpeedLines = 14;

    Image washImage;
    Image flashImage;
    float washPulse;
    float flashTimer;
    Color flashColor = Color.white;
    float intensity = 0.35f;

    Transform worldRoot;
    Sprite sparkSprite;
    Sprite lineSprite;
    Sprite softSprite;

    readonly List<Spark> sparks = new List<Spark>(MaxParticles);
    readonly List<Ghost> ghosts = new List<Ghost>(MaxGhosts);
    readonly List<SpeedLine> lines = new List<SpeedLine>(MaxSpeedLines);

    float ghostCooldown;
    Transform dino;

    struct Spark
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector3 velocity;
        public float life;
        public float maxLife;
        public float spin;
        public bool active;
    }

    struct Ghost
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public float life;
        public bool active;
    }

    struct SpeedLine
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public float speed;
        public float life;
        public bool active;
    }

    public static void Ensure(Transform parent)
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("PsychedelicFX");
        if (parent != null)
            go.transform.SetParent(parent, false);
        go.AddComponent<PsychedelicFX>();
    }

    void Awake()
    {
        Instance = this;
        sparkSprite = MakeSoftDot(16, 0.95f);
        lineSprite = MakeLine(48, 4);
        softSprite = MakeSoftDot(32, 0.65f);
        worldRoot = new GameObject("FXWorld").transform;
        worldRoot.SetParent(transform, false);
        BuildOverlay();
        PrefillPools();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        float target = 0.28f;
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
            target = Mathf.Lerp(0.35f, 0.95f, Mathf.Clamp01((GameManager.Instance.SpeedMultiplier - 1f) / 1.15f));
        else if (GameManager.Instance != null && GameManager.Instance.InMenu)
            target = 0.42f;

        intensity = Mathf.MoveTowards(intensity, target, Time.unscaledDeltaTime * 0.6f);
        washPulse += Time.unscaledDeltaTime * (0.55f + intensity * 1.4f);

        UpdateWash();
        UpdateFlash();
        UpdateSparks();
        UpdateGhosts();
        UpdateSpeedLines();
        MaybeSpawnGhost();
        MaybeSpawnSpeedLines();
    }

    public void Burst(Vector3 worldPosition, Color color, int count = 12, float force = 3.2f)
    {
        for (int i = 0; i < count; i++)
        {
            int slot = FindFreeSpark();
            if (slot < 0)
                break;

            Spark spark = sparks[slot];
            spark.active = true;
            spark.life = spark.maxLife = Random.Range(0.28f, 0.55f);
            spark.spin = Random.Range(-360f, 360f);
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude < 0.01f)
                dir = Vector2.up;
            spark.velocity = new Vector3(dir.x, dir.y, 0f) * Random.Range(force * 0.45f, force);
            spark.transform.position = worldPosition + (Vector3)(Random.insideUnitCircle * 0.12f);
            spark.transform.localScale = Vector3.one * Random.Range(0.18f, 0.42f);
            spark.transform.gameObject.SetActive(true);
            spark.renderer.sprite = Random.value > 0.45f ? sparkSprite : softSprite;
            spark.renderer.color = Color.Lerp(color, Color.white, Random.Range(0f, 0.45f));
            sparks[slot] = spark;
        }
    }

    public void RainbowBurst(Vector3 worldPosition, int count = 18)
    {
        for (int i = 0; i < count; i++)
        {
            Color color = Color.HSVToRGB(Mathf.Repeat(i / (float)count + Time.time * 0.2f, 1f), 0.85f, 1f);
            Burst(worldPosition, color, 1, 4.2f);
        }
    }

    public void JumpTrail(Vector3 worldPosition)
    {
        Burst(worldPosition + Vector3.down * 0.2f, new Color(1f, 0.45f, 0.95f, 1f), 8, 2.4f);
        Burst(worldPosition, new Color(0.4f, 0.95f, 1f, 1f), 6, 2.8f);
    }

    public void CollectPulse(Vector3 worldPosition, Color color)
    {
        Burst(worldPosition, color, 16, 4.5f);
        Flash(color, 0.22f, 0.35f);
    }

    public void MilestonePulse()
    {
        Flash(Color.HSVToRGB(Mathf.Repeat(Time.time * 0.3f, 1f), 0.75f, 1f), 0.45f, 0.55f);
        if (dino != null)
            RainbowBurst(dino.position + Vector3.up * 0.8f, 24);
        washPulse += 2.5f;
    }

    public void NearMissZap(Vector3 worldPosition)
    {
        Burst(worldPosition, new Color(1f, 0.95f, 0.2f, 1f), 10, 3.6f);
        Flash(new Color(1f, 0.85f, 0.2f, 1f), 0.12f, 0.22f);
    }

    public void DeathFlash()
    {
        Flash(new Color(1f, 0.25f, 0.65f, 1f), 0.55f, 0.7f);
    }

    public void Flash(Color color, float strength, float duration)
    {
        flashColor = color;
        flashColor.a = Mathf.Clamp01(strength);
        flashTimer = Mathf.Max(flashTimer, duration);
    }

    public void BindDinosaur(Transform target)
    {
        dino = target;
    }

    public void Clear()
    {
        flashTimer = 0f;
        if (flashImage != null)
            flashImage.color = new Color(1f, 1f, 1f, 0f);

        for (int i = 0; i < sparks.Count; i++)
        {
            Spark spark = sparks[i];
            spark.active = false;
            if (spark.transform != null)
                spark.transform.gameObject.SetActive(false);
            sparks[i] = spark;
        }

        for (int i = 0; i < ghosts.Count; i++)
        {
            Ghost ghost = ghosts[i];
            ghost.active = false;
            if (ghost.transform != null)
                ghost.transform.gameObject.SetActive(false);
            ghosts[i] = ghost;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            SpeedLine line = lines[i];
            line.active = false;
            if (line.transform != null)
                line.transform.gameObject.SetActive(false);
            lines[i] = line;
        }
    }

    public static Color NeonSky(float score, float speedMultiplier)
    {
        float t = Time.time * (0.35f + speedMultiplier * 0.55f);
        float wave = Mathf.Repeat(score / 420f + t * 0.08f, 1f);
        Color a = Color.HSVToRGB(Mathf.Repeat(wave, 1f), 0.55f, 0.92f);
        Color b = Color.HSVToRGB(Mathf.Repeat(wave + 0.18f, 1f), 0.62f, 0.88f);
        Color c = Color.HSVToRGB(Mathf.Repeat(wave + 0.42f, 1f), 0.48f, 0.78f);
        float blend = Mathf.PingPong(t * 0.35f, 1f);
        Color mixed = Color.Lerp(Color.Lerp(a, b, blend), c, 0.35f);
        // Mezcla un poco de cielo “normal” para que no ciegue al principio.
        Color calm = Color.Lerp(new Color(0.80f, 0.90f, 0.98f), mixed, Mathf.Clamp01(0.35f + speedMultiplier * 0.35f));
        return calm;
    }

    void UpdateWash()
    {
        if (washImage == null)
            return;

        float hue = Mathf.Repeat(washPulse * 0.12f, 1f);
        Color color = Color.HSVToRGB(hue, 0.7f, 1f);
        float alpha = (0.06f + intensity * 0.14f) * (0.75f + 0.25f * Mathf.Sin(washPulse * 2.2f));
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying && !GameManager.Instance.InMenu)
            alpha *= 0.35f;
        color.a = alpha;
        washImage.color = color;
    }

    void UpdateFlash()
    {
        if (flashImage == null)
            return;

        if (flashTimer <= 0f)
        {
            flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
            return;
        }

        flashTimer -= Time.unscaledDeltaTime;
        float a = flashColor.a * Mathf.Clamp01(flashTimer * 3.5f);
        flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, a);
    }

    void UpdateSparks()
    {
        for (int i = 0; i < sparks.Count; i++)
        {
            Spark spark = sparks[i];
            if (!spark.active)
                continue;

            spark.life -= Time.deltaTime;
            if (spark.life <= 0f)
            {
                spark.active = false;
                spark.transform.gameObject.SetActive(false);
                sparks[i] = spark;
                continue;
            }

            spark.velocity += Vector3.down * (6.5f * Time.deltaTime);
            spark.velocity *= 1f - (1.8f * Time.deltaTime);
            spark.transform.position += spark.velocity * Time.deltaTime;
            spark.transform.Rotate(0f, 0f, spark.spin * Time.deltaTime);
            float k = spark.life / spark.maxLife;
            Color color = spark.renderer.color;
            color.a = k;
            spark.renderer.color = color;
            float size = Mathf.Lerp(0.06f, 0.4f, k);
            spark.transform.localScale = Vector3.one * size;
            sparks[i] = spark;
        }
    }

    void UpdateGhosts()
    {
        for (int i = 0; i < ghosts.Count; i++)
        {
            Ghost ghost = ghosts[i];
            if (!ghost.active)
                continue;

            ghost.life -= Time.deltaTime;
            if (ghost.life <= 0f || ghost.renderer.sprite == null)
            {
                ghost.active = false;
                ghost.transform.gameObject.SetActive(false);
                ghosts[i] = ghost;
                continue;
            }

            Color color = ghost.renderer.color;
            color.a = ghost.life * 0.55f;
            ghost.renderer.color = color;
            ghosts[i] = ghost;
        }
    }

    void UpdateSpeedLines()
    {
        float scroll = GameManager.Instance != null ? GameManager.Instance.CurrentSpeed : 0f;
        for (int i = 0; i < lines.Count; i++)
        {
            SpeedLine line = lines[i];
            if (!line.active)
                continue;

            line.life -= Time.deltaTime;
            Vector3 position = line.transform.position;
            position.x -= (scroll + line.speed) * Time.deltaTime;
            line.transform.position = position;

            if (line.life <= 0f || position.x < -14f)
            {
                line.active = false;
                line.transform.gameObject.SetActive(false);
                lines[i] = line;
                continue;
            }

            Color color = line.renderer.color;
            color.a = Mathf.Clamp01(line.life) * (0.25f + intensity * 0.45f);
            line.renderer.color = color;
            lines[i] = line;
        }
    }

    void MaybeSpawnGhost()
    {
        ghostCooldown -= Time.deltaTime;
        if (ghostCooldown > 0f)
            return;
        if (dino == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            return;
        if (GameManager.Instance.SpeedMultiplier < 1.15f)
            return;

        SpriteRenderer source = dino.GetComponent<SpriteRenderer>();
        if (source == null || source.sprite == null)
            return;

        int slot = FindFreeGhost();
        if (slot < 0)
            return;

        Ghost ghost = ghosts[slot];
        ghost.active = true;
        ghost.life = Mathf.Lerp(0.12f, 0.28f, intensity);
        ghost.transform.position = dino.position;
        ghost.transform.localScale = dino.localScale;
        ghost.transform.rotation = dino.rotation;
        ghost.transform.gameObject.SetActive(true);
        ghost.renderer.sprite = source.sprite;
        ghost.renderer.flipX = source.flipX;
        ghost.renderer.flipY = source.flipY;
        Color tint = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.8f, 1f), 0.85f, 1f);
        tint.a = 0.55f;
        ghost.renderer.color = tint;
        ghosts[slot] = ghost;

        ghostCooldown = Mathf.Lerp(0.08f, 0.035f, intensity);
    }

    void MaybeSpawnSpeedLines()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            return;
        if (GameManager.Instance.SpeedMultiplier < 1.25f)
            return;
        if (Random.value > intensity * 0.35f)
            return;

        int slot = FindFreeLine();
        if (slot < 0)
            return;

        SpeedLine line = lines[slot];
        line.active = true;
        line.life = Random.Range(0.25f, 0.55f);
        line.speed = Random.Range(10f, 22f);
        float y = Random.Range(-0.4f, 6.8f);
        line.transform.position = new Vector3(12f, y, 0f);
        float thickness = Random.Range(0.04f, 0.12f);
        float length = Random.Range(0.8f, 2.4f);
        line.transform.localScale = new Vector3(length, thickness, 1f);
        line.transform.gameObject.SetActive(true);
        Color color = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.5f + y * 0.05f, 1f), 0.7f, 1f);
        color.a = 0.55f;
        line.renderer.color = color;
        lines[slot] = line;
    }

    void BuildOverlay()
    {
        GameObject canvasGo = new GameObject("PsychedelicCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        washImage = CreateFullImage(canvasGo.transform, "ColorWash");
        washImage.color = new Color(1f, 0f, 1f, 0f);
        washImage.raycastTarget = false;

        flashImage = CreateFullImage(canvasGo.transform, "NeonFlash");
        flashImage.color = new Color(1f, 1f, 1f, 0f);
        flashImage.raycastTarget = false;
    }

    static Image CreateFullImage(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return image;
    }

    void PrefillPools()
    {
        for (int i = 0; i < MaxParticles; i++)
        {
            GameObject go = new GameObject("Spark");
            go.transform.SetParent(worldRoot, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sparkSprite;
            renderer.sortingOrder = 25;
            go.SetActive(false);
            sparks.Add(new Spark { transform = go.transform, renderer = renderer });
        }

        for (int i = 0; i < MaxGhosts; i++)
        {
            GameObject go = new GameObject("Ghost");
            go.transform.SetParent(worldRoot, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 1;
            go.SetActive(false);
            ghosts.Add(new Ghost { transform = go.transform, renderer = renderer });
        }

        for (int i = 0; i < MaxSpeedLines; i++)
        {
            GameObject go = new GameObject("SpeedLine");
            go.transform.SetParent(worldRoot, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = lineSprite;
            renderer.sortingOrder = -2;
            go.SetActive(false);
            lines.Add(new SpeedLine { transform = go.transform, renderer = renderer });
        }
    }

    int FindFreeSpark()
    {
        for (int i = 0; i < sparks.Count; i++)
        {
            if (!sparks[i].active)
                return i;
        }

        return -1;
    }

    int FindFreeGhost()
    {
        for (int i = 0; i < ghosts.Count; i++)
        {
            if (!ghosts[i].active)
                return i;
        }

        return -1;
    }

    int FindFreeLine()
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (!lines[i].active)
                return i;
        }

        return -1;
    }

    static Sprite MakeSoftDot(int size, float falloff)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = (size - 1) * 0.5f;
        float max = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / max;
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = Mathf.Pow(alpha, falloff * 2.2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    static Sprite MakeLine(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < height; y++)
        {
            float vy = 1f - Mathf.Abs((y / (float)(height - 1)) * 2f - 1f);
            for (int x = 0; x < width; x++)
            {
                float vx = 1f - Mathf.Abs((x / (float)(width - 1)) * 2f - 1f);
                float alpha = Mathf.Pow(vx, 0.35f) * Mathf.Pow(vy, 1.8f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 32f);
    }
}
