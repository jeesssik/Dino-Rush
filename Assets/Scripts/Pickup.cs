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
        transform.Rotate(0f, 0f, (kind == Kind.Star ? 50f : 30f) * Time.deltaTime);

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
