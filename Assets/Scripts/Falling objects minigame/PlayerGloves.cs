using UnityEngine;

public class PlayerGloves : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float xScreenLimit = 8.5f; // Stops the gloves from leaving the screen edges

    private void Update()
    {
        // Read input from Left/Right arrow keys (or A/D keys)
        float inputX = Input.GetAxisRaw("Horizontal");

        // Move horizontally
        Vector3 movement = new Vector3(inputX, 0f, 0f) * moveSpeed * Time.deltaTime;
        transform.Translate(movement);

        // Clamp the position so the gloves stay within the screen boundaries
        float clampedX = Mathf.Clamp(transform.position.x, -xScreenLimit, xScreenLimit);
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
    }
}