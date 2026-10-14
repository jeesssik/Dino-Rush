using UnityEngine;

// Premia cuando el dinosaurio pasa muy cerca de un obstáculo sin chocarlo.
public class NearMiss : MonoBehaviour
{
    Transform dino;
    Collider2D dinoCollider;
    Collider2D ownCollider;
    float closestGap = 999f;
    bool resolved;

    void Start()
    {
        Dinosaur dinosaur = FindObjectOfType<Dinosaur>();
        if (dinosaur == null)
            return;

        dino = dinosaur.transform;
        dinoCollider = dinosaur.GetComponent<Collider2D>();
        ownCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (resolved || dino == null || ownCollider == null || dinoCollider == null)
            return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            return;

        float dx = Mathf.Abs(transform.position.x - dino.position.x);
        if (dx < 0.55f)
        {
            float above = dinoCollider.bounds.min.y - ownCollider.bounds.max.y;
            float below = ownCollider.bounds.min.y - dinoCollider.bounds.max.y;
            float gap = 999f;
            if (above >= 0f)
                gap = above;
            if (below >= 0f && below < gap)
                gap = below;
            if (gap < closestGap)
                closestGap = gap;
        }

        if (transform.position.x < dino.position.x - 0.6f)
        {
            resolved = true;
            if (closestGap < 0.42f && GameManager.Instance != null)
                GameManager.Instance.AddBonus(25, dino.position + Vector3.up * 1.15f, "+25 CERCA");
        }
    }
}
