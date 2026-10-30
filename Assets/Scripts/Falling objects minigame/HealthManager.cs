using UnityEngine;

public class HealthManager : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    private bool isGameOver = false;

    private void OnEnable()
    {
        GameEventManager.OnItemCaught += HandleItemCaught;
        GameEventManager.OnGoodItemMissed += HandleGoodItemMissed;
    }

    private void OnDisable()
    {
        GameEventManager.OnItemCaught -= HandleItemCaught;
        GameEventManager.OnGoodItemMissed -= HandleGoodItemMissed;
    }

    private void Start()
    {
        currentHealth = maxHealth;
        PublishHealth();
    }

    private void HandleItemCaught(FallingObject item)
    {
        // Penalty if player catches a Bad item (Orca)
        if (item.itemType == FallingObject.ObjectType.Bad)
        {
            TakeDamage(1);
        }
    }

    private void HandleGoodItemMissed(FallingObject item)
    {
        // Penalty if a Good item (Seal) misses the gloves and hits the bottom
        if (item.itemType == FallingObject.ObjectType.Good)
        {
            TakeDamage(1);
        }
    }

    private void TakeDamage(int amount)
    {
        if (isGameOver) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        PublishHealth();

        Debug.Log("Health lost! Current Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            TriggerGameOver();
        }
    }

    private void PublishHealth()
    {
        // The UI listens for this - health logic stays out of the UI script
        GameEventManager.TriggerHealthChanged(currentHealth, maxHealth);
    }

    private void TriggerGameOver()
    {
        isGameOver = true;

        // Publish the fail state - freezing and UI are the listeners' job
        GameEventManager.TriggerGameOver();
    }
}
