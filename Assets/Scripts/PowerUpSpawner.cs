using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private GameObject powerUpPrefab;
    [SerializeField] private float spawnInterval = 30f;

    [Header("Spawn Areas (2 zones)")]
    [SerializeField] private Collider[] spawnAreas = new Collider[2];

    private float spawnTimer = 0f;
    private bool isGamePlaying = false;

    private void Start()
    {
        if (powerUpPrefab == null)
            Debug.LogError("PowerUpSpawner: Power-Up prefab not assigned!");

        if (spawnAreas == null || spawnAreas.Length == 0)
            Debug.LogError("PowerUpSpawner: No spawn areas assigned!");
    }

    private void Update()
    {
        if (OrderManager.Instance != null)
        {
            bool gamePlayingNow = OrderManager.Instance.state == OrderManager.GameState.Playing;

            if (gamePlayingNow && !isGamePlaying)
            {
                isGamePlaying = true;
                spawnTimer = 0f;
            }
            else if (!gamePlayingNow && isGamePlaying)
            {
                isGamePlaying = false;
                spawnTimer = 0f;
            }
        }

        if (!isGamePlaying)
            return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnRandomPowerUp();
        }
    }

    private void SpawnRandomPowerUp()
    {
        if (powerUpPrefab == null || spawnAreas == null || spawnAreas.Length == 0)
            return;

        // 🔥 Pick random collider
        Collider chosenArea = spawnAreas[Random.Range(0, spawnAreas.Length)];

        if (chosenArea == null)
            return;

        Vector3 spawnPosition = GetRandomPointInCollider(chosenArea);

        Instantiate(
            powerUpPrefab,
            spawnPosition,
            powerUpPrefab.transform.rotation
        );

        Debug.Log($"Power-Up spawned in {chosenArea.name} at {spawnPosition}");
    }

    private Vector3 GetRandomPointInCollider(Collider col)
    {
        Bounds bounds = col.bounds;

        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        float y = bounds.center.y;

        return new Vector3(randomX, y, randomZ);
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnAreas == null) return;

        Gizmos.color = Color.cyan;

        foreach (var col in spawnAreas)
        {
            if (col == null) continue;

            Bounds bounds = col.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}