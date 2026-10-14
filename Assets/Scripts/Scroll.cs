using UnityEngine;

public class Scroll : MonoBehaviour
{
    private float scrollSpeed = GameManager.BaseSpeed;

    void Update()
    {
        float speed = scrollSpeed;
        if (GameManager.Instance != null)
            speed = GameManager.Instance.CurrentSpeed;

        if (speed > 0f)
            transform.Translate(Vector2.left * speed * Time.deltaTime);

        if (CompareTag("Obstacle") && transform.position.x < -10f)
            Destroy(gameObject);
    }
}
