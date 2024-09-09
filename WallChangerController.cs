using UnityEngine;
using System.Collections;

public class WallChangerController : MonoBehaviour
{
    public float minTime = 5f;  // Minimum time before the wall appears
    public float maxTime = 35f; // Maximum time before the wall appears
    public float visibleTime = 10f; // Time the wall stays visible

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private MrPickleAI mrPickleAI; // Reference to Mr. Pickle's AI script

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();

        if (spriteRenderer == null || boxCollider == null)
        {
            Debug.LogError("SpriteRenderer or BoxCollider2D component is missing.");
            return;
        }

        mrPickleAI = FindObjectOfType<MrPickleAI>(); // Get reference to the MrPickleAI script

        // Start with the wall hidden
        spriteRenderer.enabled = false;
        boxCollider.enabled = false;

        StartCoroutine(ToggleVisibility());
    }

    IEnumerator ToggleVisibility()
    {
        while (true)
        {
            // Wait for a random time before showing the wall
            float waitTime = Random.Range(minTime, maxTime);
            yield return new WaitForSeconds(waitTime);

            // Show the wall
            spriteRenderer.enabled = true;
            boxCollider.enabled = true;

            // Wait for the wall to be visible for a set time
            yield return new WaitForSeconds(visibleTime);

            // Hide the wall again
            spriteRenderer.enabled = false;
            boxCollider.enabled = false;
        }
    }
}