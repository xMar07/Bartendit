using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [Header("Item Prefabs")]
    [SerializeField] private GameObject[] goodItemPrefabs; // e.g., Seals
    [SerializeField] private GameObject[] badItemPrefabs;  // e.g., Orcas
    [SerializeField] private GameObject[] coinPrefabs;     // e.g., Coins

    [Header("Spawn Chances")]
    [Range(0f, 1f)]
    [Tooltip("Probability (0-1) that a spawn is a coin instead of a regular item. Kept low so coins stay rare.")]
    [SerializeField] private float coinChance = 0.1f;

    [Range(0f, 1f)]
    [Tooltip("Probability (0-1) that a non-coin spawn is a Good item instead of a Bad one.")]
    [SerializeField] private float goodItemChance = 0.65f;

    [Header("Spawn Timing")]
    [Tooltip("Average seconds between two spawns.")]
    [SerializeField] private float spawnInterval = 1.5f;

    [Tooltip("Random +/- variation applied to the spawn interval. 0 = perfectly timed.")]
    [SerializeField] private float intervalJitter = 0.6f;

    [Header("Spawn Position Boundaries")]
    [SerializeField] private float minX = -8f; // Left boundary outside camera view
    [SerializeField] private float maxX = 8f;  // Right boundary outside camera view
    [SerializeField] private float spawnY = 6f; // Spawn height above screen top

    private float timer;
    private float nextSpawnDelay;

    private void Start()
    {
        ScheduleNextSpawn();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= nextSpawnDelay)
        {
            SpawnRandomItem();
            timer = 0f;
            ScheduleNextSpawn();
        }
    }

    private void ScheduleNextSpawn()
    {
        float minDelay = Mathf.Max(0.05f, spawnInterval - intervalJitter);
        float maxDelay = Mathf.Max(minDelay, spawnInterval + intervalJitter);
        nextSpawnDelay = Random.Range(minDelay, maxDelay);
    }

    private void SpawnRandomItem()
    {
        // Roll coins first so their rarity is independent of the good/bad split
        if (coinPrefabs != null && coinPrefabs.Length > 0 && Random.value < coinChance)
        {
            SpawnFromPool(coinPrefabs);
            return;
        }

        // Roll for the type - every spawn is independent, never a fixed pair
        bool spawnGood = Random.value < goodItemChance;

        SpawnFromPool(spawnGood ? goodItemPrefabs : badItemPrefabs);
    }

    private void SpawnFromPool(GameObject[] pool)
    {
        // Fall back to the other array if this one is empty
        if (pool == null || pool.Length == 0)
        {
            pool = pool == goodItemPrefabs ? badItemPrefabs : goodItemPrefabs;
        }

        GameObject prefabToSpawn = GetRandomPrefab(pool);
        if (prefabToSpawn == null) return;

        float randomX = Random.Range(minX, maxX);
        Instantiate(prefabToSpawn, new Vector3(randomX, spawnY, 0f), Quaternion.identity);
    }

    private GameObject GetRandomPrefab(GameObject[] prefabArray)
    {
        if (prefabArray == null || prefabArray.Length == 0) return null;
        int randomIndex = Random.Range(0, prefabArray.Length);
        return prefabArray[randomIndex];
    }
}
