using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FallingObject : MonoBehaviour
{
    public enum ObjectType { Good, Bad, Coin }

    [Header("Item Configuration")]
    public ObjectType itemType; // Set to Good for Seal, Bad for Orca in the Inspector

    [Header("Movement Settings")]
    [Tooltip("Constant downward speed in units per second. No acceleration is applied.")]
    [SerializeField] private float fallSpeed = 5f;

    [Header("Lifetime")]
    [Tooltip("Y position below which the object is destroyed.")]
    [SerializeField] private float despawnY = -10f;

    private Rigidbody2D rb;
    private float speedMultiplier = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Start at the difficulty the player has already reached
        speedMultiplier = GameEventManager.CurrentSpeedMultiplier;

        if (rb != null)
        {
            // Move purely by velocity so gravity never adds acceleration
            rb.gravityScale = 0f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // no spinning
        }
    }

    private void OnEnable()
    {
        GameEventManager.OnSpeedMultiplierChanged += HandleSpeedMultiplierChanged;
    }

    private void OnDisable()
    {
        GameEventManager.OnSpeedMultiplierChanged -= HandleSpeedMultiplierChanged;
    }

    private void HandleSpeedMultiplierChanged(float multiplier)
    {
        // Items already on screen speed up live
        speedMultiplier = multiplier;
    }

    private void FixedUpdate()
    {
        // Fully deterministic, constant speed - no curve, no ramp up
        Vector2 velocity = new Vector2(0f, -fallSpeed * speedMultiplier);

        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }
        else
        {
            // Fallback for objects without a Rigidbody2D
            transform.Translate(velocity * Time.fixedDeltaTime, Space.World);
        }
    }

    private void Update()
    {
        // Clean up if it falls off screen
        if (transform.position.y < despawnY)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if it hit the player's gloves
        if (collision.CompareTag("PlayerGloves"))
        {
            // Notify the central hub instead of talking to score/health directly
            GameEventManager.TriggerItemCaught(this);

            Destroy(gameObject);
        }
    }
}
