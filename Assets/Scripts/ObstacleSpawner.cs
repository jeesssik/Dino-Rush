using System.Collections;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] obstacles;

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (!IsPlaying())
            {
                yield return null;
                continue;
            }

            SpawnWave();

            float speed = GameManager.Instance != null ? GameManager.Instance.SpeedMultiplier : 1f;
            float minTime = Mathf.Max(0.5f, 0.78f / speed);
            float maxTime = Mathf.Max(minTime + 0.28f, 1.75f / speed);
            float wait = Random.Range(minTime, maxTime);
            float elapsed = 0f;

            while (elapsed < wait)
            {
                if (!IsPlaying())
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    void SpawnWave()
    {
        if (obstacles == null || obstacles.Length == 0)
            return;

        int index = Random.Range(0, obstacles.Length);
        GameObject prefab = obstacles[index];
        Vector3 position = transform.position;
        bool isBird = prefab.name.Contains("Pterodactyl");

        if (isBird)
            position.y += BirdHeight();

        GameObject obstacle = Instantiate(prefab, position, Quaternion.identity);
        PrepareObstacle(obstacle);

        // A veces arma un cactus ancho, como los grupos del juego original.
        if (!isBird && Random.value < 0.2f && speedIsFair())
        {
            Vector3 extraPosition = position + Vector3.right * Random.Range(0.55f, 0.95f);
            GameObject extra = Instantiate(prefab, extraPosition, Quaternion.identity);
            PrepareObstacle(extra);
        }

        if (Random.value < 0.42f)
            SpawnPickup(position);
    }

    static void PrepareObstacle(GameObject obstacle)
    {
        if (obstacle.GetComponent<NearMiss>() == null)
            obstacle.AddComponent<NearMiss>();
    }

    void SpawnPickup(Vector3 obstaclePosition)
    {
        Pickup.Kind kind = Random.value < 0.22f ? Pickup.Kind.Shield : Pickup.Kind.Star;
        float height = kind == Pickup.Kind.Shield
            ? Random.Range(1.35f, 1.9f)
            : Random.Range(1.45f, 2.25f);
        float ahead = Random.Range(-0.35f, 0.85f);
        Pickup.Create(obstaclePosition + new Vector3(ahead, height, 0f), kind);
    }

    static float BirdHeight()
    {
        float roll = Random.value;
        if (roll < 0.5f)
            return Random.Range(0f, 0.35f);
        if (roll < 0.78f)
            return Random.Range(0.5f, 0.95f);
        return Random.Range(1.2f, 1.6f);
    }

    bool speedIsFair()
    {
        return GameManager.Instance == null || GameManager.Instance.SpeedMultiplier < 1.75f;
    }

    static bool IsPlaying()
    {
        return GameManager.Instance == null || GameManager.Instance.IsPlaying;
    }
}
