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

    public void SetSpawnAreas(Collider[] areas)
    {
        spawnAreas = areas;
    }

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
            bool gamePlayingNow = OrderManager.Instance.IsRunning;

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

        if (MatchRules.PowerUpIntervalMultiplier <= 0f)
            return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval * MatchRules.PowerUpIntervalMultiplier)
        {
            spawnTimer = 0f;
            SpawnRandomPowerUp();
        }
    }

    private void SpawnRandomPowerUp()
    {
        if (powerUpPrefab == null || spawnAreas == null || spawnAreas.Length == 0)
            return;

        for (int attempt = 0; attempt < spawnAttempts; attempt++)
        {
            Collider chosenArea = spawnAreas[Random.Range(0, spawnAreas.Length)];
            if (chosenArea == null)
                continue;

            Vector3 spawnPosition = GetRandomPointInCollider(chosenArea);
            if (!IsOpenFloorBelow(spawnPosition))
                continue;

            Instantiate(
                powerUpPrefab,
                spawnPosition,
                powerUpPrefab.transform.rotation
            );
            return;
        }
    }

    [Header("Floor Check")]
    [SerializeField] private int spawnAttempts = 30;
    [Tooltip("Free space needed around the drop column, so it never lands on or wedges against furniture")]
    [SerializeField] private float clearanceRadius = 0.7f;
    [Tooltip("Anything this low counts as floor (counter tops are ~1 high)")]
    [SerializeField] private float maxFloorHeight = 0.2f;

    private bool IsOpenFloorBelow(Vector3 point)
    {
        Vector3 rayStart = new Vector3(point.x, Mathf.Max(point.y, 10f), point.z);
        if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 30f, ~0, QueryTriggerInteraction.Ignore))
            return false;

        if (hit.point.y > maxFloorHeight)
            return false;

        Vector3 bottom = new Vector3(point.x, hit.point.y + 0.1f + clearanceRadius, point.z);
        Vector3 top = new Vector3(point.x, Mathf.Max(point.y, bottom.y), point.z);
        return !Physics.CheckCapsule(bottom, top, clearanceRadius, ~0, QueryTriggerInteraction.Ignore);
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