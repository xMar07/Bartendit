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

    [Header("Despawn")]
    [Tooltip("Y position below which a missed item is considered gone (below the gloves).")]
    [SerializeField] private float despawnY = -7f;

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
            SpawnFromPool(coinPrefabs, FallingObject.ObjectType.Coin);
            return;
        }

        // Roll for the type - every spawn is independent, never a fixed pair
        bool spawnGood = Random.value < goodItemChance;

        SpawnFromPool(spawnGood ? goodItemPrefabs : badItemPrefabs,
                      spawnGood ? FallingObject.ObjectType.Good : FallingObject.ObjectType.Bad);
    }

    private void SpawnFromPool(GameObject[] pool, FallingObject.ObjectType type)
    {
        // Fall back to the other array if this one is empty
        if (pool == null || pool.Length == 0)
        {
            pool = pool == goodItemPrefabs ? badItemPrefabs : goodItemPrefabs;
        }

        GameObject prefabToSpawn = GetRandomPrefab(pool);
        if (prefabToSpawn == null) return;

        float randomX = Random.Range(minX, maxX);
        GameObject spawned = Instantiate(prefabToSpawn,
                                         new Vector3(randomX, spawnY, 0f),
                                         Quaternion.identity);

        // Make sure it has a FallingObject so the event managers can read itemType.
        FallingObject fallingObject = spawned.GetComponent<FallingObject>();
        if (fallingObject == null)
        {
            fallingObject = spawned.AddComponent<FallingObject>();
        }
        fallingObject.itemType = type;

        // Ensure a Collider2D exists so trigger/collision callbacks can fire.
        if (spawned.GetComponent<Collider2D>() == null)
        {
            spawned.AddComponent<BoxCollider2D>();
        }

        // Ensure a Rigidbody2D exists — required for 2D physics callbacks when
        // the gloves collider is static (no Rigidbody2D of its own).
        Rigidbody2D rb = spawned.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = spawned.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.simulated = true;

        // Attach the lightweight handler that fires the events + destroys the object.
        SpawnedItemHandler handler = spawned.AddComponent<SpawnedItemHandler>();
        handler.Init(fallingObject, despawnY, type);
    }

    private GameObject GetRandomPrefab(GameObject[] prefabArray)
    {
        if (prefabArray == null || prefabArray.Length == 0) return null;
        int randomIndex = Random.Range(0, prefabArray.Length);
        return prefabArray[randomIndex];
    }
}

// ======================================================================
// Added at runtime to every spawned object. Handles BOTH trigger and
// collision callbacks so it works regardless of how the gloves collider
// is configured. Fires GameEventManager events and destroys itself.
//
// Key fixes over the previous version:
//   - Stores the type locally and re-applies it to the FallingObject right
//     before invoking, guaranteeing the listener sees the correct enum.
//   - Invokes are wrapped so a throwing listener cannot prevent Destroy.
// ======================================================================
[RequireComponent(typeof(Collider2D))]
public class SpawnedItemHandler : MonoBehaviour
{
    private FallingObject fallingObject;
    private FallingObject.ObjectType itemType;
    private float despawnY;
    private bool resolved;

    public void Init(FallingObject item, float despawn, FallingObject.ObjectType type)
    {
        fallingObject = item;
        itemType = type;
        despawnY = despawn;

        // Apply immediately, and keep applying — protects against any prefab
        // script that overwrites itemType in its own Awake/Start.
        ApplyType();
    }

    private void Start()
    {
        // Late safety net: one frame later, ensure the type is still correct.
        ApplyType();
    }

    private void ApplyType()
    {
        if (fallingObject != null)
        {
            fallingObject.itemType = itemType;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryResolveHit(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryResolveHit(collision.collider);
    }

    private void TryResolveHit(Component other)
    {
        if (resolved) return;
        if (other == null) return;
        if (!other.CompareTag("Gloves")) return;

        resolved = true;

        // Guarantee the listener sees the right type, even if something
        // reset it between Init and now.
        ApplyType();

        // Raise the event. Wrap in try/catch so an exception in any listener
        // cannot skip Destroy(gameObject) below.
        try
        {
            GameEventManager.TriggerItemCaught(fallingObject);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SpawnedItemHandler] Listener threw during TriggerItemCaught: {e}");
        }

        Destroy(gameObject);
    }

    private void Update()
    {
        if (resolved) return;

        if (transform.position.y < despawnY)
        {
            resolved = true;

            if (itemType == FallingObject.ObjectType.Good)
            {
                try
                {
                    GameEventManager.TriggerGoodItemMissed(fallingObject);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SpawnedItemHandler] Listener threw during TriggerGoodItemMissed: {e}");
                }
            }

            Destroy(gameObject);
        }
    }
}