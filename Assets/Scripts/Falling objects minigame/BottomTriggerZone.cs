using UnityEngine;

public class BottomTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the object entering the bottom zone is a falling item
        FallingObject fallingItem = collision.GetComponent<FallingObject>();
        
        if (fallingItem != null)
        {
            // If a Good item (Seal) reaches the bottom without being caught
            if (fallingItem.itemType == FallingObject.ObjectType.Good)
            {
                GameEventManager.TriggerGoodItemMissed(fallingItem);
            }

            // Destroy the object so it doesn't pile up
            Destroy(collision.gameObject);
        }
    }
}