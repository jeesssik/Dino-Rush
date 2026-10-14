using UnityEngine;

public class Pickup : MonoBehaviour
{
    public enum Kind
    {
        Star,
        Shield
    }

    Kind kind;
    float baseY;
    float phase;
    bool taken;
    SpriteRenderer body;
    SpriteRenderer aura;
    float sparkTimer;

    public static void Create(Vector3 position, Kind kind)
    {
        GameObject go = new GameObject(kind == Kind.Star ? "Star" : "ShieldPickup");
        go.transform.position = position;
        go.transform.localScale = Vector3.one * (kind == Kind.Star ? 0.72f : 0.62f);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = kind == Kind.Star ? GameSprites.Star : GameSprites.Orb;
        renderer.sortingOrder = 4;

        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.7f;

        Pickup pickup = go.AddComponent<Pickup>();
        pickup.kind = kind;
        pickup.baseY = position.y;
        pickup.phase = Random.Range(0f, Mathf.PI * 2f);
        pickup.body = renderer;
        pickup.CreateAura();
    }

    void CreateAura()
    {
        GameObject glow = new GameObject("Aura");
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale = Vector3.one * 1.8f;
        aura = glow.AddComponent<SpriteRenderer>();
        aura.sprite = GameSprites.Orb;
        aura.sortingOrder = 3;
        aura.color = kind == Kind.Star
            ? new Color(1f, 0.85f, 0.2f, 0.35f)
            : new Color(0.35f, 0.9f, 1f, 0.35f);
    }

    void Update()
    {
        float speed = GameManager.Instance != null ? GameManager.Instance.CurrentSpeed : 0f;
        if (speed <= 0f)
            return;

        Vector3 position = transform.position;
        position.x -= speed * Time.deltaTime;
        position.y = baseY + Mathf.Sin(Time.time * 4.5f + phase) * 0.1f;
        transform.position = position;
        transform.Rotate(0f, 0f, (kind == Kind.Star ? 90f : 50f) * Time.deltaTime);

        float pulse = 1f + Mathf.Sin(Time.time * 8f + phase) * 0.12f;
        transform.localScale = Vector3.one * ((kind == Kind.Star ? 0.72f : 0.62f) * pulse);

        if (body != null && kind == Kind.Star)
        {
            Color tint = Color.HSVToRGB(Mathf.Repeat(Time.time * 1.4f + phase, 1f), 0.75f, 1f);
            body.color = tint;
        }

        if (aura != null)
        {
            float auraPulse = 1.6f + Mathf.Sin(Time.time * 6f + phase) * 0.35f;
            aura.transform.localScale = Vector3.one * auraPulse;
            Color c = aura.color;
            c.a = 0.2f + 0.25f * Mathf.Abs(Mathf.Sin(Time.time * 5f + phase));
            if (kind == Kind.Star)
            {
                Color rainbow = Color.HSVToRGB(Mathf.Repeat(Time.time * 1.2f, 1f), 0.8f, 1f);
                rainbow.a = c.a;
                aura.color = rainbow;
            }
            else
            {
                aura.color = c;
            }
        }

        sparkTimer -= Time.deltaTime;
        if (sparkTimer <= 0f && PsychedelicFX.Instance != null)
        {
            sparkTimer = Random.Range(0.12f, 0.28f);
            Color sparkColor = kind == Kind.Star
                ? Color.HSVToRGB(Mathf.Repeat(Time.time, 1f), 0.85f, 1f)
                : new Color(0.35f, 0.95f, 1f, 1f);
            PsychedelicFX.Instance.Burst(transform.position, sparkColor, 2, 1.2f);
        }

        if (position.x < -12f)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (taken)
            return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            return;
        if (other.GetComponent<Dinosaur>() == null)
            return;

        taken = true;
        if (kind == Kind.Star)
            GameManager.Instance.CollectStar(transform.position);
        else
            GameManager.Instance.CollectShield(transform.position);

        Destroy(gameObject);
    }
}
